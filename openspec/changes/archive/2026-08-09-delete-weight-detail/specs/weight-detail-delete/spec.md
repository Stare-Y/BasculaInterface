## ADDED Requirements

### Requirement: Delete a captured weight detail via a guarded endpoint
The system SHALL expose `PATCH /api/Weight/Detail/{id}/Delete` to soft-delete an existing `WeightDetail` (`IsDeleted=true`), separate from the existing unguarded `DELETE /api/Weight/Detail?id=` endpoint. This endpoint SHALL NOT modify any other `WeightDetail` field besides `IsDeleted` and `LastUpdated`.

#### Scenario: Successfully delete a captured detail
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` with the correct `PasswordHash`, for an existing, not-yet-deleted `WeightDetail`
- **THEN** `detail.IsDeleted` is set to `true`, the detail no longer appears in the parent `WeightEntry`'s detail list, and the server returns `200 OK`

#### Scenario: Reject deleting a detail that does not exist or is already deleted
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` for an `id` with no matching non-deleted `WeightDetail`
- **THEN** the server returns `404 Not Found`

### Requirement: Password gate on guarded detail deletion
The system SHALL require a `PasswordHash` in the request body, compared against the same shared password hash used for changing a weight detail's product, partner, and amount (`WeightSettings.ChangeProductPasswordHash`). The client SHALL hash the operator-entered plaintext password before sending it; the server SHALL never receive or need the plaintext.

#### Scenario: Reject on incorrect password
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` with a `PasswordHash` that does not match the configured hash
- **THEN** the server returns `400 Bad Request` with a message indicating an incorrect password, and the detail is not deleted

### Requirement: BruteWeight recomputed only when the deleted detail was loaded
The system SHALL recompute `WeightEntry.BruteWeight` after a successful guarded deletion, if and only if the deleted detail had `IsLoaded == true` at the time of deletion. Deleting a never-loaded detail SHALL NOT trigger a `BruteWeight` recomputation.

#### Scenario: BruteWeight recomputed after deleting a loaded detail
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` with the correct password, for a detail with `IsLoaded == true`
- **THEN** `WeightEntry.BruteWeight` is recomputed as `TareWeight + Σ(IsLoaded, non-deleted details).Weight`, excluding the just-deleted detail

#### Scenario: BruteWeight unchanged after deleting a never-loaded detail
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` with the correct password, for a detail with `IsLoaded == false`
- **THEN** `WeightEntry.BruteWeight` is not recomputed

### Requirement: Applies even on a concluded weight entry, blocked once an ERP document exists
`PATCH /api/Weight/Detail/{id}/Delete` SHALL NOT reject the request when the parent `WeightEntry.ConcludeDate` is set. It SHALL reject the request when `WeightEntry.ConptaqiComercialFK > 0`, regardless of `ConcludeDate`.

#### Scenario: Successfully delete a detail belonging to a concluded entry
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` with the correct password, for a detail whose parent `WeightEntry` has `ConcludeDate != null` and `ConptaqiComercialFK` null or 0
- **THEN** the detail is deleted and the server returns `200 OK`

#### Scenario: Reject when the entry already has a Contpaqi document
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` with the correct password, for a detail whose parent `WeightEntry` has `ConptaqiComercialFK > 0`
- **THEN** the server returns `400 Bad Request` with a message indicating the entry already has an ERP document, and the detail is not deleted

### Requirement: No minimum-detail-count floor
The system SHALL NOT reject a guarded deletion on the basis that it would leave the parent `WeightEntry` with zero remaining (non-deleted) details.

#### Scenario: Deleting the last remaining detail succeeds
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` with the correct password, for the only non-deleted `WeightDetail` on its parent `WeightEntry`
- **THEN** the detail is deleted, the server returns `200 OK`, and the parent `WeightEntry` is left with zero non-deleted details

### Requirement: No credit re-validation on deletion
The system SHALL NOT invoke partner-credit validation as part of a guarded deletion, since removing a detail only decreases the parent `WeightEntry`'s cost exposure.

#### Scenario: Deletion succeeds regardless of the partner's current credit standing
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Delete` with the correct password, for a detail whose parent `WeightEntry`'s partner is already at or over their credit limit
- **THEN** the detail is deleted and the server returns `200 OK` without performing a credit check

### Requirement: Concurrency protection consistent with other detail mutations
The endpoint SHALL use the same optimistic-concurrency handling as the other `Detail/{id}/...` endpoints: a concurrent modification of the parent `WeightEntry` during the `BruteWeight` recompute step SHALL result in `409 Conflict`.

#### Scenario: Concurrent modification during recompute returns a conflict
- **WHEN** two terminals concurrently mutate the same `WeightEntry`, one via `PATCH .../Delete` on a loaded detail, and the resulting `BruteWeight` recompute loses the optimistic-concurrency check
- **THEN** the server returns `409 Conflict` with a message indicating the record was modified by another terminal

### Requirement: Existing unguarded delete endpoint is unaffected
The pre-existing `DELETE /api/Weight/Detail?id={id}` endpoint SHALL continue to operate exactly as before: no password required, no `BruteWeight` recompute, no ERP-document or concluded-entry guard.

#### Scenario: Unguarded endpoint still deletes without a password
- **WHEN** a terminal sends `DELETE /api/Weight/Detail?id={id}` for an existing detail
- **THEN** the detail is soft-deleted with no password check and no `BruteWeight` recomputation, exactly as before this change

### Requirement: Row menu offers a delete action, gated to the main terminal
The MAUI client's row-level "⋮" menu SHALL offer an "Eliminar" action alongside the existing "Cambiar producto"/"Cambiar socio"/"Cambiar peso" (or "Cambiar cantidad") actions, under the same visibility rule (`CanChangeProductMenu`: not `SecondaryTerminal`, not `OnlyFinished`).

#### Scenario: Row menu shows all four actions in the styled popup
- **WHEN** an operator taps the "⋮" button on a weight-detail row where `CanChangeProductMenu` is true
- **THEN** the themed popup lists "Cambiar producto", "Cambiar socio", "Cambiar peso"/"Cambiar cantidad", and "Eliminar" as distinct, styled options, plus a way to cancel

#### Scenario: Selecting "Eliminar" prompts for confirmation and a password before deleting
- **WHEN** an operator selects "Eliminar" from the row menu
- **THEN** a confirmation popup appears showing the detail's description and a password field, and no deletion occurs until the operator confirms with a password
