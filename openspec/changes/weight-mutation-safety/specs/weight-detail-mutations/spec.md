## ADDED Requirements

### Requirement: Create weight detail via dedicated endpoint
The system SHALL expose `POST /api/Weight/Detail` to add a product slot to an existing WeightEntry. The detail SHALL be created with `IsLoaded=true` (default, for main-terminal flow) and `Weight=0`. The endpoint SHALL reject the request if the WeightEntry does not exist or is already concluded.

#### Scenario: Successfully create a detail
- **WHEN** a terminal sends `POST /api/Weight/Detail` with a valid `FK_WeightEntryId`, `FK_WeightedProductId`, and `RequiredAmount`
- **THEN** a new `WeightDetail` is persisted with `Weight=0`, `Tare=0`, `SecondaryTare=null`, `IsLoaded=true`, and the server returns the created detail with its assigned `Id`

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
The system SHALL expose `PUT /api/Weight/Detail/{id}/Weight` to record the net measured weight of a product batch. The server SHALL set `detail.Tare` to the current live `BruteWeight` of the parent WeightEntry at the time of the write (not from client). The endpoint SHALL NOT change `IsLoaded`.

#### Scenario: Successfully record weight
- **WHEN** a terminal sends `PUT /api/Weight/Detail/{id}/Weight` with a positive `Weight` value and `WeightedBy` string
- **THEN** `detail.Weight` and `detail.WeightedBy` are persisted; `detail.Tare` is set to the server's current `WeightEntry.BruteWeight`; `IsLoaded` is unchanged; server returns `200 OK`

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
The existing `PUT /api/Weight` SHALL continue to accept updates for non-integrity fields: `Notes`, `VehiclePlate`, `PartnerId`, `ExternalTargetBehaviorFK`, `RegisteredBy`. It SHALL silently ignore (not error on) any `BruteWeight`, `TareWeight`, `ConcludeDate`, or detail weight values sent in the payload — these fields can only be modified through their dedicated endpoints.

#### Scenario: Notes update succeeds and preserves BruteWeight
- **WHEN** a terminal sends `PUT /api/Weight` with updated `Notes` and any `BruteWeight` value
- **THEN** `Notes` is updated; `BruteWeight` in the database is unchanged
