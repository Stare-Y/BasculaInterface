# User Authentication — Spec

## Purpose

Defines the custom `User` entity (no ASP.NET Core Identity, no external provider) and the login mechanics against it: unique identifiers, salted password hashing, and JWT issuance on success. Introduced by issue #134.

## Requirements

### Requirement: Custom User entity with unique identifiers
The system SHALL expose a `User` entity with a unique `Username`, a unique `UserCode` (alphanumeric only — letters and/or digits, no other characters), a salted password hash, and a `Role`. `User` SHALL use the same `BaseEntity` soft-delete (`IsDeleted`) convention as every other entity; a soft-deleted user SHALL be treated as disabled.

#### Scenario: UserCode must be unique and alphanumeric
- **WHEN** an admin attempts to create a user with a `UserCode` that already exists, or that contains a character other than a letter or digit
- **THEN** the request is rejected and no user is created

#### Scenario: Disabled user cannot authenticate
- **WHEN** a user with `IsDeleted == true` attempts to log in with correct credentials
- **THEN** authentication fails as if the credentials were invalid

### Requirement: Login resolves UserCode first, then Username
`POST /api/Auth/Login` SHALL accept a single identifier field and a plaintext password. The server SHALL first attempt to match the identifier against `UserCode`; if no non-deleted user matches, it SHALL attempt to match against `Username`.

#### Scenario: Login by UserCode
- **WHEN** a client submits an identifier that matches an existing user's `UserCode` and the correct password
- **THEN** that user is authenticated

#### Scenario: Login by Username when UserCode does not match
- **WHEN** a client submits an identifier that does not match any `UserCode` but matches an existing user's `Username`, with the correct password
- **THEN** that user is authenticated

#### Scenario: No match on either field
- **WHEN** a client submits an identifier that matches neither a `UserCode` nor a `Username`
- **THEN** the server returns `401 Unauthorized` without revealing which field failed to match

### Requirement: Password verification uses a salted hash, never a client-computed comparison
Passwords SHALL be stored using a per-user salted hash (PBKDF2). The server SHALL verify the plaintext password submitted at login against the stored salted hash. The client SHALL NOT pre-hash the password before sending it.

#### Scenario: Correct password authenticates
- **WHEN** a client submits the correct plaintext password for a resolved user
- **THEN** the server verifies it against that user's stored salted hash and authentication succeeds

#### Scenario: Incorrect password is rejected
- **WHEN** a client submits an incorrect plaintext password for a resolved user
- **THEN** the server returns `401 Unauthorized` and no token is issued

### Requirement: Successful login issues a JWT
On successful authentication, the server SHALL issue a JWT containing the user's id, username, usercode, and role, with a configurable expiry (default 12 hours), along with the user's effective permission flags in the response body.

#### Scenario: Login response includes token and permissions
- **WHEN** a user authenticates successfully
- **THEN** the response includes a JWT and the user's effective `CanSelfAuthorizeGate`/`CanCaptureWeightManually` values
