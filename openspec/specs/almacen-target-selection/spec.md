# Almacén Target Selection — Spec

## Purpose

Defines how a pedido-line-to-weight conversion picks which ERP almacén (warehouse) the resulting Contpaqi document targets, reusing the existing `ExternalTargetBehavior` table (hidden rows) instead of introducing a separate almacén catalog.

## Requirements

### Requirement: Hidden ExternalTargetBehavior rows are the almacén targets
The system SHALL NOT introduce a separate almacén catalog entity. The almacén target for a pedido conversion SHALL be one of the existing `ExternalTargetBehavior` rows where `Hidden == true`. `ExternalTargetBehavior` SHALL gain a nullable `AlmacenName` string that labels its `TargetAlmacen` code for display. `GET /api/ExternalTargetBehavior/AlmacenTargets` SHALL return the hidden rows, ordered by `TargetAlmacen`.

#### Scenario: Listing almacén targets
- **WHEN** a client calls `GET /api/ExternalTargetBehavior/AlmacenTargets`
- **THEN** the server returns every non-deleted `ExternalTargetBehavior` with `Hidden == true`, each carrying `Id`, `TargetAlmacen`, `AlmacenName`, `TargetSerie`, `TargetName`

#### Scenario: Non-hidden behaviors are untouched
- **WHEN** a client calls the existing `GET /api/ExternalTargetBehavior/Available`
- **THEN** it still returns only the non-hidden rows, unchanged by this feature

### Requirement: Almacén target selection at conversion is an explicit operator choice
When converting a pedido line to a **new** weight entry, the operator SHALL explicitly select an almacén target from the hidden `ExternalTargetBehavior` list. The selection SHALL be pre-filled with the row whose `TargetAlmacen` matches the product's classification-derived default (`ProductoDto.IdAlmacen`) when one matches, but SHALL always be confirmed by the operator. If the list is empty the client SHALL surface an error and SHALL NOT submit. The chosen behavior becomes the new `WeightEntry.ExternalTargetBehaviorFK`. When a line is converted onto an **existing** open weight entry, that entry's target is kept and the parameter is ignored.

#### Scenario: Operator confirms the pre-filled default
- **WHEN** the operator opens the convert dialog for a product whose `IdAlmacen` is `"2"`, and a hidden behavior with `TargetAlmacen == "2"` exists
- **THEN** that row is pre-selected, and on confirmation `POST /api/Pedido/Line/{id}/ConvertToWeight` is called with `ExternalTarget` set to its id

#### Scenario: New weight entry rejects a missing or non-hidden target
- **WHEN** `POST /api/Pedido/Line/{id}/ConvertToWeight` is called with no `WeightEntryId` and an `ExternalTarget` that is empty, unparseable, or refers to a non-hidden behavior
- **THEN** the server responds 400 (`InvalidOperationException`) and no weight entry is created

#### Scenario: Empty target list blocks conversion
- **WHEN** the operator opens the convert dialog and `GET /api/ExternalTargetBehavior/AlmacenTargets` returns no rows
- **THEN** the client shows an error telling the operator to configure the hidden behaviors, and no conversion request is sent

#### Scenario: Appending to an open entry keeps its target
- **WHEN** `POST /api/Pedido/Line/{id}/ConvertToWeight` is called with a `WeightEntryId` for an open entry
- **THEN** the new `WeightDetail` is appended and the entry's existing `ExternalTargetBehaviorFK` is unchanged

### Requirement: ERP almacén resolution is unchanged from before this change
The system SHALL resolve the almacén for Contpaqi document submission as `product.IdAlmacen ?? weightEntry.ExternalTargetBehavior.TargetAlmacen` — exactly as it did before this feature. The `WeightDetail`-level almacén override introduced by the first implementation is removed; `WeightDetail` no longer has an `FK_AlmacenId`.

#### Scenario: Granel product keeps its hardcoded almacén
- **WHEN** a weight detail's product has `CIDVALORCLASIFICACION6 == 9` (so `IdAlmacen == "1"`)
- **THEN** the Contpaqi document uses almacén `"1"` regardless of the picked behavior's `TargetAlmacen`

#### Scenario: Non-granel product uses the picked behavior's almacén
- **WHEN** a weight detail's product has no hardcoded `IdAlmacen`, and the entry's `ExternalTargetBehavior.TargetAlmacen` is `"21P"`
- **THEN** the Contpaqi document uses almacén `"21P"`
