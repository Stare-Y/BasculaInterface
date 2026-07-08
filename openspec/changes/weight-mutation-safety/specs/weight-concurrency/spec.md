## ADDED Requirements

### Requirement: WeightEntry uses optimistic concurrency via xmin
The system SHALL configure `WeightEntry` in EF Core to use the PostgreSQL `xmin` system column as a concurrency token (`UseXminAsConcurrencyToken()`). When two concurrent writes target the same `WeightEntry` row, the second write SHALL receive a `DbUpdateConcurrencyException`, which the service layer SHALL translate to an HTTP `409 Conflict` response.

#### Scenario: Concurrent writes detected and rejected
- **WHEN** two terminals concurrently submit writes to the same WeightEntry and the second write's `xmin` no longer matches the row's current `xmin`
- **THEN** the server returns `409 Conflict` for the second write; the first write's changes are preserved in the database

#### Scenario: No xmin conflict on sequential writes
- **WHEN** a terminal writes to a WeightEntry and then a second terminal writes after the first completes
- **THEN** both writes succeed; the second terminal's `xmin` snapshot reflects the post-first-write state

### Requirement: MAUI client retries narrow mutations silently on 409
The MAUI `ApiService` SHALL implement a `PutWithRetryAsync` helper used for all narrow weight-integrity mutation endpoints (`SecondaryTare`, `Weight`, `MarkLoaded`, `Conclude`). On receiving a `409 Conflict` response, the client SHALL: wait 300ms, re-fetch the latest `WeightEntryDto` from the server, reconstruct the request payload using the fresh server state, and retry the mutation. The client SHALL retry up to 3 times before propagating the error.

#### Scenario: Single retry succeeds transparently
- **WHEN** a terminal receives `409 Conflict` on a `MarkLoaded` call due to a concurrent write
- **THEN** the client waits 300ms, re-fetches the entry, retries `MarkLoaded` — the user sees no error alert; the operation completes successfully

#### Scenario: Three retries exhausted surfaces error to user
- **WHEN** a terminal receives `409 Conflict` on 3 consecutive retry attempts
- **THEN** the client shows a user-facing alert: "El servidor no pudo procesar la solicitud, intente de nuevo" and the operation is not applied

#### Scenario: 409 on non-mutation endpoints is not retried
- **WHEN** a `GET` request or the general `PUT /api/Weight` (non-integrity) returns any status
- **THEN** normal error handling applies; `PutWithRetryAsync` is NOT used for these paths
