## MODIFIED Requirements

### Requirement: Six fixed roles
The system SHALL define exactly six roles: `Operator`, `Dispatching Operator`, `Supervisor`, `Admin`, `Sudo`, and `Customer Service`. `Customer Service` SHALL be stored at the same underlying ordinal previously used for `Purchasing Operator` — a rename only, appended after `Sudo`, never inserted earlier, so every existing stored `Role` value is unaffected.

#### Scenario: A user is assigned exactly one role
- **WHEN** a user is created or edited
- **THEN** it is assigned exactly one of the six defined roles

#### Scenario: Existing stored roles are unaffected by the rename
- **WHEN** a `User` row whose stored `Role` value previously resolved to `Purchasing Operator` is read after this change
- **THEN** it resolves to `Customer Service`, with identical role-derived behavior (same `TerminalMode` default, same permission defaults, same inactivity timeout default)

### Requirement: Role-based default permissions
Each role SHALL have a default value for two permission flags, `CanSelfAuthorizeGate` and `CanCaptureWeightManually`: `Operator`, `Dispatching Operator`, `Admin`, and `Customer Service` default both to `false`; `Supervisor` defaults both to `true`; `Sudo` bypasses both checks unconditionally (see the separate bypass requirement).

#### Scenario: A Supervisor has gate access by default
- **WHEN** a `Supervisor` user with no explicit override is evaluated for `CanSelfAuthorizeGate`
- **THEN** the effective value is `true`

#### Scenario: An Operator lacks gate access by default
- **WHEN** an `Operator` user with no explicit override is evaluated for `CanSelfAuthorizeGate`
- **THEN** the effective value is `false`

#### Scenario: A Customer Service user lacks gate access by default
- **WHEN** a `Customer Service` user with no explicit override is evaluated for `CanSelfAuthorizeGate`
- **THEN** the effective value is `false`
