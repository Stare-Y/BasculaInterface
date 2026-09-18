# Spec Delta

## Purpose

Defines terminal identities that can be pinned to a forced role, the role-rank ordering used to gate login at such a terminal, and the boundary between what a forced role controls (rendered workflow) and what it does not (the logged-in user's own permissions).

## ADDED Requirements

### Requirement: A terminal identity can be configured with a forced role
The system SHALL support registering a terminal identity, each optionally assigned a single forced role from the six existing roles (`Operator`, `Dispatching Operator`, `Supervisor`, `Admin`, `Sudo`, `Customer Service`). A terminal with no forced role assigned is unconfigured for this capability.

#### Scenario: A newly registered terminal has no forced role
- **WHEN** a terminal identity is registered without an explicit forced role
- **THEN** it has no forced role, and is treated as unconfigured for this capability

#### Scenario: A terminal is assigned a forced role
- **WHEN** an `Admin` or `Sudo` sets a terminal's forced role to `Dispatching Operator`
- **THEN** that terminal's forced role is `Dispatching Operator`

### Requirement: Only Admin or Sudo can configure a terminal's forced role
Configuring or clearing a terminal's forced role SHALL require the caller's role to be `Admin` or `Sudo`, consistent with the existing user-administration access rule.

#### Scenario: A non-admin cannot configure a terminal's forced role
- **WHEN** an authenticated user whose role is `Operator`, `Dispatching Operator`, `Supervisor`, or `Customer Service` attempts to set or clear a terminal's forced role
- **THEN** the server returns `403 Forbidden`

### Requirement: Role rank for terminal access gating
The system SHALL define a role rank, ordered lowest to highest, distinct from the `Role` enum's stored ordinal values and used only for the terminal login gate described below: `Dispatching Operator` (lowest), `Customer Service`, `Operator`, `Supervisor`, `Admin`, `Sudo` (highest). Introducing or reordering this rank SHALL NOT alter the `Role` enum's stored ordinal values.

#### Scenario: A higher-ranked role compares above a lower-ranked role
- **WHEN** comparing the rank of `Admin` against the rank of `Operator`
- **THEN** `Admin`'s rank is higher

#### Scenario: Sudo is the highest rank
- **WHEN** comparing the rank of `Sudo` against any other role
- **THEN** `Sudo`'s rank is highest

### Requirement: Login is gated by role rank at a terminal with a forced role
Authenticating at a terminal whose forced role is configured SHALL succeed only if the authenticating user's role rank is greater than or equal to the terminal's forced role's rank. A terminal with no forced role configured SHALL NOT apply this gate.

#### Scenario: A user ranked at or above the terminal's forced role logs in successfully
- **WHEN** a user with valid credentials and role rank greater than or equal to the terminal's configured forced role's rank authenticates at that terminal
- **THEN** authentication succeeds

#### Scenario: A user ranked below the terminal's forced role is denied
- **WHEN** a user with valid credentials and role rank lower than the terminal's configured forced role's rank authenticates at that terminal
- **THEN** authentication fails and no token is issued, even though the credentials themselves are correct

#### Scenario: An unconfigured terminal applies no rank gate
- **WHEN** a user with valid credentials authenticates at a terminal with no forced role configured
- **THEN** authentication succeeds or fails based solely on credential validity, unaffected by role rank

### Requirement: A terminal's forced role determines rendered workflow only, not the user's own permissions
A terminal's forced role SHALL determine only the resolved `TerminalMode` for that login (see `terminal-mode-assignment`). It SHALL NOT affect the logged-in user's effective `CanSelfAuthorizeGate` or `CanCaptureWeightManually`, which continue to resolve from that user's own role and per-user overrides exactly as defined in `role-permission-model`.

#### Scenario: Permission flags are unaffected by a terminal's forced role
- **WHEN** a user with effective `CanSelfAuthorizeGate = true` logs in at a terminal whose forced role's role-default for that flag would otherwise be `false`
- **THEN** that user's effective `CanSelfAuthorizeGate` remains `true`
