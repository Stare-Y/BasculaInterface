## MODIFIED Requirements

### Requirement: Six fixed roles
The system SHALL define exactly six roles: `Operator`, `Dispatching Operator`, `Supervisor`, `Admin`, `Sudo`, and `Purchasing Operator`. `Purchasing Operator` SHALL be appended after `Sudo` in the underlying enum, never inserted earlier, so every existing stored `Role` value is unaffected.

#### Scenario: A user is assigned exactly one role
- **WHEN** a user is created or edited
- **THEN** it is assigned exactly one of the six defined roles

#### Scenario: Existing stored roles are unaffected by the new role's addition
- **WHEN** a `User` row created before this requirement existed is read after this change
- **THEN** its `Role` resolves to the exact same one of the original five roles as before, unchanged
