# Weight Detail Mutations — Spec

## Purpose

Defines the narrow, dedicated endpoints for mutating `WeightDetail` fields and maintaining `WeightEntry.BruteWeight` as a server-owned value. Replaces the previous pattern of sending full `WeightEntry` snapshots through `PUT /api/Weight`.

## Requirements

### Requirement: Create weight detail via dedicated endpoint
The system SHALL expose `POST /api/Weight/Detail` to add a product slot to an existing WeightEntry. When `Weight > 0` is provided in the request, the server SHALL persist it and recompute `BruteWeight` atomically. The detail SHALL be created with `IsLoaded=true` (default). The endpoint SHALL reject the request if the WeightEntry does not exist or is already concluded.

#### Scenario: Successfully create a detail (main-terminal flow, weight provided at creation)
- **WHEN** a terminal sends `POST /api/Weight/Detail` with a valid `FK_WeightEntryId` and `Weight > 0`
- **THEN** a new `WeightDetail` is persisted with the provided `Weight`, `Tare` set to the entry's current `BruteWeight`, `IsLoaded=true`; `WeightEntry.BruteWeight` is recomputed; server returns the created detail

#### Scenario: Successfully create a detail (secondary-terminal flow, weight deferred)
- **WHEN** a terminal sends `POST /api/Weight/Detail` with a valid `FK_WeightEntryId` and `Weight=0`
- **THEN** a new `WeightDetail` is persisted with `Weight=0`, `Tare=0`, `IsLoaded=true`; `BruteWeight` is unchanged; server returns the created detail with its assigned `Id`

#### Scenario: Reject detail creation on concluded entry
- **WHEN** a terminal sends `POST /api/Weight/Detail` for a `WeightEntry` with `ConcludeDate != null`
- **THEN** the server returns `400 Bad Request` with message indicating the entry is already concluded

### Requirement: Set secondary tare via dedicated endpoint
The system SHALL expose `PUT /api/Weight/Detail/{id}/SecondaryTare` to record the empty-tarima weight for a detail. Setting the secondary tare SHALL atomically set `IsLoaded=false` on that detail, signaling that the product is entering the secondary weighing flow and must not be counted in BruteWeight until explicitly marked loaded.

#### Scenario: Successfully set secondary tare
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/SecondaryTare` with a positive `SecondaryTare` value
- **THEN** the detail is updated with `SecondaryTare=<value>` and `IsLoaded=false`, and the server returns `200 OK`

#### Scenario: Reject zero or negative secondary tare
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/SecondaryTare` with a value ≤ 0
- **THEN** the server returns `400 Bad Request`

#### Scenario: Reject on concluded entry
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/SecondaryTare` for a detail belonging to a concluded WeightEntry
- **THEN** the server returns `400 Bad Request`

### Requirement: Record measured weight via dedicated endpoint
The system SHALL expose `PUT /api/Weight/Detail/{id}/Weight` to record the net measured weight of a product batch. The server SHALL set `detail.Tare` to the current live `BruteWeight` of the parent WeightEntry at the time of the write (not from client). If the detail is already `IsLoaded=true` (main-terminal pre-registration flow), the server SHALL also recompute `WeightEntry.BruteWeight` after recording the weight.

#### Scenario: Successfully record weight (detail IsLoaded=false — secondary terminal flow)
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/Weight` for a detail with `IsLoaded=false`
- **THEN** `detail.Weight` and `detail.WeightedBy` are persisted; `detail.Tare` is set to the server's current `WeightEntry.BruteWeight`; `IsLoaded` remains `false`; `BruteWeight` unchanged; server returns `200 OK`

#### Scenario: Successfully record weight (detail IsLoaded=true — main terminal pre-registration flow)
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/Weight` for a detail with `IsLoaded=true`
- **THEN** `detail.Weight`, `detail.WeightedBy`, and `detail.Tare` are persisted; `WeightEntry.BruteWeight` is recomputed as `TareWeight + Σ(IsLoaded details).Weight`; server returns `200 OK`

#### Scenario: Reject zero or negative weight
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/Weight` with `Weight <= 0`
- **THEN** the server returns `400 Bad Request`

#### Scenario: Reject on concluded entry
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/Weight` for a detail on a concluded entry
- **THEN** the server returns `400 Bad Request`

### Requirement: Mark detail as loaded via dedicated endpoint
The system SHALL expose `PUT /api/Weight/Detail/{id}/MarkLoaded` to transition a detail from `IsLoaded=false` to `IsLoaded=true`. The server SHALL validate that `Weight > 0` and `IsLoaded=false` before accepting the transition. After setting `IsLoaded=true`, the server SHALL recompute `WeightEntry.BruteWeight = TareWeight + Σ(IsLoaded=true details).Weight` atomically in the same transaction.

#### Scenario: Successfully mark as loaded and recompute BruteWeight
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/MarkLoaded` for a detail with `Weight > 0` and `IsLoaded=false`
- **THEN** `detail.IsLoaded` is set to `true`; `WeightEntry.BruteWeight` is recomputed as `TareWeight + Σ(IsLoaded details).Weight`; server returns `200 OK` with updated `WeightEntryDto`

#### Scenario: Reject if weight not yet recorded
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/MarkLoaded` for a detail with `Weight == 0`
- **THEN** the server returns `400 Bad Request` with message indicating weight must be recorded first

#### Scenario: Reject if already loaded
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/MarkLoaded` for a detail already `IsLoaded=true`
- **THEN** the server returns `400 Bad Request` (IsLoaded is one-way; no silent no-op)

#### Scenario: Reject on concluded entry
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/MarkLoaded` for a detail on a concluded entry
- **THEN** the server returns `400 Bad Request`

### Requirement: General PUT /api/Weight rejects integrity fields
The existing `PUT /api/Weight` SHALL continue to accept updates for non-integrity fields: `Notes`, `VehiclePlate`, `PartnerId`, `ExternalTargetBehaviorFK`, `RegisteredBy`, `TareWeight`. It SHALL recompute `BruteWeight` server-side when `TareWeight` changes. It SHALL silently ignore `ConcludeDate` and detail weight values sent in the payload.

#### Scenario: Notes update succeeds and preserves BruteWeight
- **WHEN** a terminal sends `PUT /api/Weight` with updated `Notes` and any `BruteWeight` value
- **THEN** `Notes` is updated; `BruteWeight` in the database is unchanged

#### Scenario: TareWeight update recomputes BruteWeight
- **WHEN** a terminal sends `PUT /api/Weight` with an updated `TareWeight`
- **THEN** `TareWeight` is saved; `BruteWeight` is recomputed as `TareWeight + Σ(IsLoaded details).Weight`
