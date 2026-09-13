# User Administration — Spec

## Purpose

Defines the Admin/Sudo-only user-management API introduced by issue #134: creating, editing, and disabling `User` rows, per-user permission overrides, and the `Name`/`LastName` fields added afterward — required going forward through the API but nullable at the database level so pre-existing production rows stay valid.

## Requirements

### Requirement: Only Admin or Sudo can manage users
Endpoints to create, edit, or disable a `User` SHALL require the caller's role to be `Admin` or `Sudo`.

#### Scenario: A non-admin cannot create a user
- **WHEN** an authenticated user whose role is `Operator`, `Dispatching Operator`, or `Supervisor` calls a user-management endpoint
- **THEN** the server returns `403 Forbidden`

#### Scenario: An Admin can create a user
- **WHEN** an authenticated `Admin` calls the create-user endpoint with a unique `Username`/`UserCode`, a password, a role, and a non-blank `Name`/`LastName`
- **THEN** a new `User` is created

### Requirement: Admin can set per-user permission overrides
The user-management API SHALL allow an `Admin` or `Sudo` to set each of `CanSelfAuthorizeGateOverride`/`CanCaptureWeightManuallyOverride` to `true`, `false`, or null (inherit role default) for any user.

#### Scenario: Overriding a flag for a specific user
- **WHEN** an `Admin` sets `CanSelfAuthorizeGateOverride = true` for an `Operator` user
- **THEN** that user's effective `CanSelfAuthorizeGate` becomes `true`

### Requirement: Disabling a user soft-deletes it
Disabling a user SHALL set `IsDeleted = true` on that `User`, consistent with the rest of the codebase's soft-delete convention. A disabled user SHALL fail both login and the self-authorize gate.

#### Scenario: A disabled user cannot log in or pass the gate
- **WHEN** a user with `IsDeleted == true` attempts to log in, or is presented as `GateIdentifier` at the self-authorize gate
- **THEN** both operations fail as if the credentials were invalid

### Requirement: User has an optional-at-storage, required-at-creation name
`User` SHALL have `Name` and `LastName` fields. Both SHALL be nullable at the database level, so existing rows created before this requirement existed remain valid without modification. The user-creation endpoint SHALL require both to be present and non-blank; the update endpoint SHALL leave either field unchanged when omitted from the request, and reject a blank value when one is supplied.

#### Scenario: Creating a user without a Name is rejected
- **WHEN** an `Admin` or `Sudo` calls the create-user endpoint with a missing or blank `Name`
- **THEN** the server returns `400 Bad Request` and no user is created

#### Scenario: Creating a user without a LastName is rejected
- **WHEN** an `Admin` or `Sudo` calls the create-user endpoint with a missing or blank `LastName`
- **THEN** the server returns `400 Bad Request` and no user is created

#### Scenario: Creating a user with both fields present succeeds
- **WHEN** an `Admin` or `Sudo` calls the create-user endpoint with a non-blank `Name` and `LastName`, alongside the other required fields
- **THEN** a new `User` is created with those values stored

#### Scenario: A pre-existing row without a Name or LastName remains valid
- **WHEN** a `User` row created before this requirement existed (with `NULL` `Name`/`LastName`) is read, logged in with, or used at the self-authorize gate
- **THEN** the operation succeeds exactly as it did before, reporting `null` for `Name`/`LastName` in the API response

#### Scenario: Updating a user without supplying Name or LastName leaves them unchanged
- **WHEN** an `Admin` or `Sudo` calls the update-user endpoint without including `Name` or `LastName` in the request
- **THEN** the user's stored `Name`/`LastName` (including a pre-existing `null`) are left unchanged

#### Scenario: Updating a user with a blank Name or LastName is rejected
- **WHEN** an `Admin` or `Sudo` calls the update-user endpoint with `Name` or `LastName` present but blank/whitespace-only
- **THEN** the server returns `400 Bad Request` and the stored value is left unchanged
