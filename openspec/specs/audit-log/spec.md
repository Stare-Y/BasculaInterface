# Audit Log — Spec

## Purpose

Defines audit trail recording for mutating actions against the core weighing/pedido entities, and the `Radiography` endpoint that surfaces a `WeightEntry`'s full history (including soft-deleted details) alongside its audit trail. Introduced by issue #134 once user identity existed to attribute actions to.

## Requirements

### Requirement: Every mutating action is recorded
Each mutating action against `WeightEntry`, `WeightDetail`, `Pedido`, or `PedidoLine` SHALL write an `AuditLogEntry` recording the acting user's id, a UTC timestamp, an action name, and the target entity's type and id.

#### Scenario: Deleting a weight entry is audited
- **WHEN** an authenticated user successfully deletes a `WeightEntry`
- **THEN** an `AuditLogEntry` is written with that user's id, the current UTC time, an action identifying the delete, and the entry's id

#### Scenario: A rejected action is not recorded as successful
- **WHEN** a mutating action is rejected (e.g. gate check fails, entity not found)
- **THEN** no `AuditLogEntry` is written implying the action succeeded

### Requirement: Radiography endpoint returns a WeightEntry with its details and audit trail
`GET /api/Weight/{id}/Radiography` SHALL return the `WeightEntry` regardless of its `IsDeleted` state, all of its `WeightDetail`s including logically-deleted ones, and every `AuditLogEntry` recorded against that entry or any of its details, ordered by timestamp.

#### Scenario: Radiography includes soft-deleted details
- **WHEN** a `WeightEntry` has one `WeightDetail` with `IsDeleted == true`
- **THEN** the radiography response includes that detail alongside the non-deleted ones

#### Scenario: Radiography includes the full audit trail for the entry and its details
- **WHEN** a `WeightEntry` and one of its details have three combined audit entries recorded against them
- **THEN** the radiography response includes all three, ordered by timestamp

#### Scenario: Radiography for a nonexistent entry
- **WHEN** `GET /api/Weight/{id}/Radiography` is called with an id matching no `WeightEntry` at all (deleted or not)
- **THEN** the server returns `404 Not Found`
