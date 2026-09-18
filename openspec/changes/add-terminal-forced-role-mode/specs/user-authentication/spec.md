# Spec Delta

## MODIFIED Requirements

### Requirement: Login resolves UserCode first, then Username
`POST /api/Auth/Login` SHALL accept a single identifier field, a plaintext password, and a terminal identifier. The server SHALL first attempt to match the identifier against `UserCode`; if no non-deleted user matches, it SHALL attempt to match against `Username`. The terminal identifier is used solely to resolve the terminal's configured forced role and apply the role-rank gate defined in `terminal-role-enforcement`; it plays no part in credential resolution.

#### Scenario: Login by UserCode
- **WHEN** a client submits an identifier that matches an existing user's `UserCode` and the correct password
- **THEN** that user is authenticated

#### Scenario: Login by Username when UserCode does not match
- **WHEN** a client submits an identifier that does not match any `UserCode` but matches an existing user's `Username`, with the correct password
- **THEN** that user is authenticated

#### Scenario: No match on either field
- **WHEN** a client submits an identifier that matches neither a `UserCode` nor a `Username`
- **THEN** the server returns `401 Unauthorized` without revealing which field failed to match
