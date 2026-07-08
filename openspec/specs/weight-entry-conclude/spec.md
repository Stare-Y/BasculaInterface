# Weight Entry Conclude — Spec

## Purpose

Defines how a WeightEntry is finalized. Conclusion is server-owned and irreversible. After conclusion the entry is immutable and downstream systems (ProviderPurchase, ContpaqiComercial) are notified.

## Requirements

### Requirement: Conclude weight entry via dedicated endpoint
The system SHALL expose `PUT /api/Weight/{id}/Conclude` to finalize a WeightEntry. The server SHALL validate all preconditions before setting `ConcludeDate` and triggering downstream actions. Once concluded, the entry is immutable.

#### Scenario: Successfully conclude a complete entry
- **WHEN** a terminal sends `PUT /api/Weight/{id}/Conclude` for an entry where all WeightDetails have `IsLoaded=true` and a `PartnerId` is set (if multiple details exist)
- **THEN** `ConcludeDate` is set to `DateTime.UtcNow`; the server triggers `ProviderPurchaseService.ConcludeByWeightEntryAsync`; if `ExternalTargetBehaviorFK` is set and `PartnerId` is set, the ContpaqiComercial post is attempted; server returns `200 OK`

#### Scenario: Reject conclude if any detail is not loaded
- **WHEN** a terminal sends `PUT /api/Weight/{id}/Conclude` and any WeightDetail has `IsLoaded=false`
- **THEN** the server returns `400 Bad Request` with a message identifying the unloaded product

#### Scenario: Reject conclude if multiple details and no partner
- **WHEN** a terminal sends `PUT /api/Weight/{id}/Conclude` and `WeightDetails.Count > 1` and `PartnerId` is null or 0
- **THEN** the server returns `400 Bad Request` requiring a partner before concluding

#### Scenario: Reject conclude on already-concluded entry
- **WHEN** a terminal sends `PUT /api/Weight/{id}/Conclude` for an entry with `ConcludeDate != null`
- **THEN** the server returns `400 Bad Request`

#### Scenario: Contpaqi failure does not roll back conclusion
- **WHEN** the Contpaqi post fails during conclude
- **THEN** `ConcludeDate` remains set (entry is concluded); the error is logged; the API response includes a warning message but returns `200 OK` (Contpaqi can be retried separately)
