## ADDED Requirements

### Requirement: Operator-declared dis-taring on the pedido line

The system SHALL store a `RequiresDisTaring` boolean on each `PedidoLine` (default `false`), set by the operator when creating or editing the line, purely as an informational note visible on the order. There SHALL be no product-level dis-taring configuration table and no dependency on contpaqi classification fields — the declaration is per pedido line.

#### Scenario: New line defaults to not requiring dis-taring
- **WHEN** a `PedidoLine` is created without specifying `RequiresDisTaring`
- **THEN** its stored `RequiresDisTaring` is `false`

#### Scenario: Operator marks a line as requiring dis-taring
- **WHEN** the operator sets `RequiresDisTaring = true` on a line via `POST`/`PUT /api/Pedido/Line`
- **THEN** the value is persisted on that line and returned by `GET /api/Pedido/{id}` for that line, and shown as a badge/checkbox in the Pedido form

### Requirement: The flag does not reach WeightDetail or the weighing screen

**Revised (2026-09-04):** an earlier pass of this change also stored `RequiresDisTaring` on `WeightDetail`, inherited at conversion. The owner reverted this after confirming it never drove any behavior on the weighing screen — it was a pure data-carrier. `WeightDetail` SHALL NOT have a `RequiresDisTaring` (or equivalent) column. Converting a `PedidoLine` to weight SHALL NOT carry any dis-tare flag onto the resulting `WeightDetail`. The pre-existing manual `WeightDetail.SecondaryTare` / `WeightService.SetSecondaryTareAsync` mechanism — gated by the `SecondaryTerminal` preference and an already-set `SecondaryTare` value, not by any per-line/per-product flag — remains the only dis-tare capture path, completely unchanged by this feature.

#### Scenario: Converting a flagged line doesn't touch WeightDetail
- **WHEN** a `PedidoLine` with `RequiresDisTaring = true` is converted to weight
- **THEN** the resulting `WeightDetail` is created with no dis-tare field at all; `SetSecondaryTareAsync` remains the only, explicitly-invoked mechanism for dis-taring, unaffected by the line's flag

#### Scenario: Non-pedido weigh-in is unaffected
- **WHEN** a `WeightDetail` is created through any flow that does not originate from a `PedidoLine`
- **THEN** nothing about its creation changes from before this whole change — no dis-tare-related field exists on it to set
