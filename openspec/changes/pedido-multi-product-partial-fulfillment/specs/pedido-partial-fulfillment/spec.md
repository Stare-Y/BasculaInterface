## ADDED Requirements

### Requirement: A pedido line can be converted to weight repeatedly
The system SHALL allow `POST /api/Pedido/Line/{id}/ConvertToWeight` to be called more than once for the same `PedidoLine`, as long as its computed pending amount is greater than zero. Each call creates a new `WeightDetail` with `FK_PedidoLineId` set to the line, either attached to an existing open `WeightEntry` (when `WeightEntryId` is supplied) or a newly created one.

#### Scenario: First partial conversion
- **WHEN** a line with `RequiredAmount=10000` and no prior weighing receives `POST .../ConvertToWeight` with `TargetAmount=5000`
- **THEN** a new `WeightDetail` is created with `RequiredAmount=5000` and `FK_PedidoLineId` set to the line, and the line's computed pending amount becomes `5000` once that detail is weighed and loaded

#### Scenario: Second conversion after a partial delivery
- **WHEN** the same line, now with pending `5000`, receives a second `POST .../ConvertToWeight` call
- **THEN** the call succeeds (no "already has a weight entry" rejection), creating an independent `WeightDetail`, and the line's pending amount can reach `0` once that detail is weighed and loaded

#### Scenario: Converting without a target amount defaults to the full pending amount
- **WHEN** `POST .../ConvertToWeight` is called with no `TargetAmount`
- **THEN** the created `WeightDetail`'s `RequiredAmount` equals the line's current pending amount

#### Scenario: Rejecting an over-large target amount
- **WHEN** `POST .../ConvertToWeight` is called with `TargetAmount` greater than the line's current pending amount
- **THEN** the server returns `400 Bad Request` and no `WeightDetail` is created

### Requirement: Received and pending amounts are computed, not stored
The system SHALL compute a `PedidoLine`'s received amount as the sum of `Weight` across all its non-deleted, `IsLoaded` `WeightDetail`s, and its pending amount as `RequiredAmount` minus that sum, at query time. No separate counter field SHALL be persisted or independently updated for these values.

#### Scenario: Received amount reflects all contributing details
- **WHEN** a line has two loaded `WeightDetail`s weighing `3000` and `4000` against a `RequiredAmount` of `10000`
- **THEN** `GET /api/Pedido/{pedidoId}` reports that line's received amount as `7000` and pending amount as `3000`

#### Scenario: A never-loaded detail does not count toward received amount
- **WHEN** a `WeightDetail` linked to a line has `IsLoaded=false`
- **THEN** its `Weight` is excluded from that line's computed received amount

### Requirement: Line conclusion is automatic on full receipt, or manual via force-close
A `PedidoLine` SHALL be considered concluded when its computed pending amount reaches zero or below, or when explicitly force-closed via `PATCH /api/Pedido/Line/{id}/Close`. A concluded line SHALL reject further `ConvertToWeight` calls.

#### Scenario: Line concludes automatically once fully received
- **WHEN** a line's computed pending amount reaches `0` after a conversion's detail is weighed and loaded
- **THEN** the line's `Concluded` value becomes `true` and subsequent `ConvertToWeight` calls for it are rejected with `400 Bad Request`

#### Scenario: Manually closing a line with remaining pending amount
- **WHEN** `PATCH /api/Pedido/Line/{id}/Close` is called on a line with pending amount `2000`
- **THEN** the line's `Concluded` value becomes `true` immediately, and further `ConvertToWeight` calls for it are rejected

### Requirement: Pedido header conclusion is derived from its lines
A `Pedido` header's `Concluded` value SHALL be computed as true only when every one of its (non-deleted) lines is concluded (naturally or via force-close). No separate stored header-level flag SHALL be maintained.

#### Scenario: Header remains open while any line is pending
- **WHEN** a pedido has two lines, one concluded and one with pending amount `> 0`
- **THEN** `GET /api/Pedido/{id}` reports the header's `Concluded` as `false`

#### Scenario: Header concludes once all lines conclude
- **WHEN** every line of a pedido becomes concluded (naturally or via force-close)
- **THEN** `GET /api/Pedido/{id}` reports the header's `Concluded` as `true`
