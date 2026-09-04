## ADDED Requirements

### Requirement: WeightEntry carries an explicit discharge-direction flag

The system SHALL store a `IsDischarge` boolean on `WeightEntry` (default `false`). `true` means the vehicle arrived already loaded and is discharging into the almacén (e.g. a provider delivery), rather than the usual case of arriving empty and being loaded. It SHALL be set automatically to `true` when a new `WeightEntry` is created via `PedidoService.ConvertLineToWeightAsync` — every pedido delivery is a discharge. All other entry-creation paths (manual weigh-ins, "Solo Pedidos" appends to an existing open entry) leave it at its existing value.

#### Scenario: A pedido conversion creates a discharge entry
- **WHEN** `POST /api/Pedido/Line/{id}/ConvertToWeight` is called with no `WeightEntryId`, creating a new `WeightEntry`
- **THEN** the created entry has `IsDischarge = true`

#### Scenario: A normal manual weigh-in is unaffected
- **WHEN** a `WeightEntry` is created through any non-pedido flow
- **THEN** its `IsDischarge` is `false`, exactly as `BruteWeight` behaved before this change

### Requirement: BruteWeight counts down for a discharge entry instead of up

The system SHALL compute `WeightEntry.BruteWeight` as `TareWeight - Σ(loaded WeightDetail.Weight)` when `IsDischarge` is `true`, and `TareWeight + Σ(loaded WeightDetail.Weight)` otherwise — replacing the previous unconditional addition in `WeightRepo.RecomputeBruteWeightAsync`, `MarkDetailLoadedAsync`, and `UpdateAsync`. The per-product `WeightDetail.Weight` capture (`Math.Abs` of consecutive scale readings) is unaffected by this change — it already produces the correct discharged amount regardless of direction.

#### Scenario: Discharging a product lowers BruteWeight
- **WHEN** a discharge entry has `TareWeight = 10000` and a `WeightDetail` with `Weight = 3000` and `IsLoaded = true` is recomputed
- **THEN** `BruteWeight` becomes `7000`, not `13000`

#### Scenario: A normal load entry is unchanged
- **WHEN** a non-discharge entry has `TareWeight = 0` and a `WeightDetail` with `Weight = 3000` and `IsLoaded = true` is recomputed
- **THEN** `BruteWeight` becomes `3000`, exactly as before this change

#### Scenario: Over-discharge is rejected
- **WHEN** a discharge entry's loaded `WeightDetail` weights sum to more than `TareWeight`
- **THEN** the recompute throws `InvalidOperationException` instead of producing a negative `BruteWeight`

### Requirement: The printed ticket reflects the entry's direction

`PrintService`'s weight-entry ticket SHALL label the initial and final weight lines according to `entry.IsDischarge`, using the same underlying `TareWeight`/`BruteWeight` values (no other layout change):
- Header "initial weight" line: `"TARA INICIAL:"` normally, `"PESO INICIAL (CARGADO):"` when `IsDischarge`.
- Footer "final weight" line: `"BRUTO:"` normally, `"PESO FINAL (VACÍO):"` when `IsDischarge`.

The `"Proveedor:"/"Socio:"` partner-role line is unaffected — it stays keyed off the partner's own `IsProvider` classification, not this entry's direction. Per-product `"Tara:"/"Neto:"` rows are unaffected either way.

#### Scenario: A discharge ticket shows the correct labels and a falling total
- **WHEN** a discharge entry's ticket is printed
- **THEN** it shows `"PESO INICIAL (CARGADO):"` with the higher arrival weight and `"PESO FINAL (VACÍO):"` with the lower departure weight — never presented as an increase

#### Scenario: A normal ticket is unchanged
- **WHEN** a non-discharge entry's ticket is printed
- **THEN** it shows `"TARA INICIAL:"` and `"BRUTO:"` exactly as before this change
