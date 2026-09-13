## ADDED Requirements

### Requirement: Only Admin or Sudo can manage users
Endpoints to create, edit, or disable a `User` SHALL require the caller's role to be `Admin` or `Sudo`.

#### Scenario: A non-admin cannot create a user
- **WHEN** an authenticated user whose role is `Operator`, `Dispatching Operator`, or `Supervisor` calls a user-management endpoint
- **THEN** the server returns `403 Forbidden`

#### Scenario: An Admin can create a user
- **WHEN** an authenticated `Admin` calls the create-user endpoint with a unique `Username`/`UserCode`, a password, and a role
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
