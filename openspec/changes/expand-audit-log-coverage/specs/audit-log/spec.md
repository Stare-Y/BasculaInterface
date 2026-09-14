## MODIFIED Requirements

### Requirement: Every mutating action is recorded
Each mutating action against `WeightEntry`, `WeightDetail`, `Pedido`, `PedidoLine`, or `User` SHALL write an `AuditLogEntry` recording the acting user's id, a UTC timestamp, an action name, and the target entity's type and id. This applies to the full weighing lifecycle (creation, adding a detail, recording a weight, marking a detail loaded, concluding, ERP submission), the full pedido lifecycle (creation, creating a line, closing a line, converting a line to a weight entry), and user management (creation, update, disable) — not only the delete/change actions that also require the self-authorize gate.

#### Scenario: Deleting a weight entry is audited
- **WHEN** an authenticated user successfully deletes a `WeightEntry`
- **THEN** an `AuditLogEntry` is written with that user's id, the current UTC time, an action identifying the delete, and the entry's id

#### Scenario: Creating a weight entry is audited
- **WHEN** an authenticated user successfully creates a `WeightEntry`
- **THEN** an `AuditLogEntry` is written identifying the creation and the new entry's id

#### Scenario: Recording a weight is audited
- **WHEN** an authenticated user successfully records a weight against a `WeightDetail`
- **THEN** an `AuditLogEntry` is written identifying that action and the detail's id

#### Scenario: Concluding a weight entry is audited
- **WHEN** an authenticated user successfully concludes a `WeightEntry`
- **THEN** an `AuditLogEntry` is written identifying the conclusion and the entry's id

#### Scenario: User management actions are audited
- **WHEN** an Admin or Sudo user successfully creates, updates, or disables a `User`
- **THEN** an `AuditLogEntry` is written identifying that action and the affected user's id

#### Scenario: A rejected action is not recorded as successful
- **WHEN** a mutating action is rejected (e.g. gate check fails, entity not found, validation fails)
- **THEN** no `AuditLogEntry` is written implying the action succeeded

### Requirement: An audit-write failure never affects the action it's attached to
Recording an `AuditLogEntry` SHALL NOT be able to cause the mutating action it's attached to to fail or appear to fail. A failure while writing the audit row SHALL be logged, not raised to the caller.

#### Scenario: An audit-write failure does not fail the underlying action
- **WHEN** the underlying data store for `AuditLogEntry` fails while recording an otherwise-successful mutating action
- **THEN** the mutating action still reports success to its caller, and the audit-write failure is logged rather than surfaced as an error on that request

### Requirement: There is exactly one delete path for a WeightDetail, and it is gated
The system SHALL expose exactly one way to delete a `WeightDetail`, and it SHALL require a valid self-authorize-gate credential.

#### Scenario: No ungated delete path exists
- **WHEN** a client attempts to delete a `WeightDetail`
- **THEN** the only available endpoint requires a `GateIdentifier`/`GatePassword` credential, verified the same way as every other gated action
