# Weight Entry Delete — Spec

## Purpose

Defines the password-gated endpoint that lets an operator soft-delete an entire `WeightEntry`, replacing the previous unguarded `DELETE /api/Weight?id=`. Reuses the same shared password as `weight-detail-product-change`, `weight-entry-partner-change`, `weight-detail-amount-change`, and `weight-detail-delete`, and extends the same "blocked once an ERP document exists" boundary those already enforce to this bigger, previously-unguarded action (issue #133 / extend-delete-password-gate).

## Requirements

### Requirement: Delete a whole WeightEntry via a guarded endpoint
The system SHALL expose `PATCH /api/Weight/{id}/Delete` to soft-delete an existing `WeightEntry` (`IsDeleted=true`), replacing the previous unguarded `DELETE /api/Weight?id=`. This endpoint SHALL NOT modify any `WeightEntry` field besides `IsDeleted`.

#### Scenario: Successfully delete a WeightEntry
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Delete` with the correct `PasswordHash`, for an existing, not-yet-deleted `WeightEntry` with no Contpaqi document
- **THEN** `entry.IsDeleted` is set to `true` and the server returns `200 OK`

#### Scenario: Reject deleting an entry that does not exist or is already deleted
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Delete` for an `id` with no matching non-deleted `WeightEntry`
- **THEN** the server returns `404 Not Found`

### Requirement: Password gate on WeightEntry deletion
The system SHALL require a `PasswordHash` in the request body, compared against the same shared password hash used for the other guarded weight-detail mutations (`WeightSettings.ChangeProductPasswordHash`). The client SHALL hash the operator-entered plaintext password before sending it; the server SHALL never receive or need the plaintext.

#### Scenario: Reject on incorrect password
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Delete` with a `PasswordHash` that does not match the configured hash
- **THEN** the server returns `400 Bad Request` with a message indicating an incorrect password, and the entry is not deleted

### Requirement: Blocked once an ERP document exists; still applies to a concluded entry
`PATCH /api/Weight/{id}/Delete` SHALL NOT reject the request when `WeightEntry.ConcludeDate` is set. It SHALL reject the request when `WeightEntry.ConptaqiComercialFK > 0`, regardless of `ConcludeDate`.

#### Scenario: Successfully delete a concluded entry with no ERP document
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Delete` with the correct password, for an entry with `ConcludeDate != null` and `ConptaqiComercialFK` null or 0
- **THEN** the entry is deleted and the server returns `200 OK`

#### Scenario: Reject when the entry already has a Contpaqi document
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Delete` with the correct password, for an entry with `ConptaqiComercialFK > 0`
- **THEN** the server returns `400 Bad Request` with a message indicating the entry already has an ERP document, and the entry is not deleted

### Requirement: Previous unguarded route is removed
The pre-existing `DELETE /api/Weight?id={id}` endpoint SHALL no longer exist. Any client still calling it SHALL receive a routing failure, not a silent unguarded delete.

#### Scenario: Old route no longer resolves
- **WHEN** a client sends `DELETE /api/Weight?id={id}`
- **THEN** the server returns a 404/405 routing response, not a successful deletion

### Requirement: MAUI client prompts for a password before deleting a WeightEntry
The `DetailedWeightView`'s delete-entry action SHALL prompt for the manager password before calling the delete endpoint, replacing the previous plain yes/no confirmation.

#### Scenario: Deleting a WeightEntry from the client requires a password
- **WHEN** an operator taps the delete-entry button on `DetailedWeightView`
- **THEN** a confirmation popup appears requesting the manager password, and no deletion occurs until a password is submitted

#### Scenario: Cancelling the password prompt aborts the deletion
- **WHEN** an operator cancels the password prompt (or submits an empty password) for deleting a WeightEntry
- **THEN** no request is sent and the entry is not deleted
