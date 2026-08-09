# Weight Entry Partner Change — Spec

## Purpose

Defines the password-gated endpoint that lets an operator change the partner (`Socio`) on an existing `WeightEntry`, with credit re-validated against the new partner and a hard block once the entry already has a related Contpaqi document. This is a deliberate, explicit override — not a general-purpose edit path — intended to correct a misassigned partner without discarding work already captured on the entry. Also covers the themed row-menu popup that surfaces this action alongside the existing product-change action.

## Requirements

### Requirement: Change the partner on an existing weight entry
The system SHALL expose `PATCH /api/Weight/{id}/Partner` to change `WeightEntry.PartnerId` to a new partner. No other field on the entry or its details SHALL be modified by this endpoint.

#### Scenario: Successfully change the partner on an entry with no captured cost yet
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Partner` with a valid `NewPartnerId` and the correct `PasswordHash`, for an entry with no weight details
- **THEN** `entry.PartnerId` is set to `NewPartnerId` and the server returns `200 OK`

#### Scenario: Successfully change the partner on an entry with captured cost
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Partner` with a valid `NewPartnerId` and the correct `PasswordHash`, for an entry whose details have captured weight/cost, and the new partner has enough available credit for that cost
- **THEN** `entry.PartnerId` is updated, no detail fields are changed, and the server returns `200 OK`

### Requirement: Password gate on partner change
The system SHALL require a `PasswordHash` in the request body, compared against the same shared password hash configured for changing a weight detail's product. The client SHALL hash the operator-entered plaintext password before sending it; the server SHALL never receive or need the plaintext.

#### Scenario: Reject on incorrect password
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Partner` with a `PasswordHash` that does not match the configured hash
- **THEN** the server returns `400 Bad Request` with a message indicating an incorrect password, and `entry.PartnerId` is unchanged

### Requirement: Blocked once the entry has a related Contpaqi document
The system SHALL reject a partner change when the entry's `ConptaqiComercialFK` is greater than 0 (a Contpaqi document already exists for this entry), regardless of whether `ConcludeDate` is set.

#### Scenario: Reject when the entry already has a Contpaqi document
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Partner` with the correct password, for an entry with `ConptaqiComercialFK > 0`
- **THEN** the server returns `400 Bad Request` with a message indicating the entry already has an ERP document, and `entry.PartnerId` is unchanged

#### Scenario: Allow the change on a concluded entry with no Contpaqi document yet
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Partner` with the correct password, for an entry with `ConcludeDate != null` and `ConptaqiComercialFK` null or 0
- **THEN** the change is applied and the server returns `200 OK`

### Requirement: Credit re-validation against the new partner's full exposure
The system SHALL re-validate the new partner's credit using the existing partner-credit validation before applying the change. The `requestedAmount` passed to that validation SHALL be the sum of `ProductPrice * quantity` across all of the entry's non-deleted details (`quantity` being `Weight` if captured, else `RequiredAmount`) — the entry's full current cost, not a delta — since none of that cost is yet attributed to the new partner.

#### Scenario: Reject when the new partner lacks sufficient credit
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Partner` with the correct password, and the entry's total cost exceeds the new partner's available credit
- **THEN** the server returns `400 Bad Request` with a message indicating insufficient credit, and `entry.PartnerId` is unchanged

#### Scenario: Allow the change when the new partner ignores credit limits or has none configured
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Partner` with the correct password, and the new partner has `IgnoreCreditLimit == true` or `CreditLimit <= 0`
- **THEN** the change is applied without a hard rejection, and the server returns `200 OK`

#### Scenario: Re-selecting the already-assigned partner does not double-count credit
- **WHEN** a terminal sends `PATCH /api/Weight/{id}/Partner` with the correct password and `NewPartnerId` equal to the entry's current `PartnerId`
- **THEN** the server does not re-validate credit for that partner (this entry's cost is already counted in their pending exposure) and returns `200 OK`

### Requirement: Concurrency protection consistent with other weight-entry mutations
The endpoint SHALL use the same optimistic-concurrency handling as other weight-entry mutations: a concurrent modification of the same entry SHALL result in `409 Conflict`.

#### Scenario: Concurrent modification returns a conflict
- **WHEN** two terminals attempt to mutate the same `WeightEntry` concurrently, one via `PATCH .../Partner`, and the resulting write loses the optimistic-concurrency check
- **THEN** the server returns `409 Conflict` with a message indicating the record was modified by another terminal

### Requirement: Row action menu is a themed popup, not a native action sheet
The row's "⋮" menu (`DetailedWeightView`) SHALL present its actions ("Cambiar producto", "Cambiar socio") through a custom, app-themed popup component consistent with the app's existing popup visual style, instead of the platform's native action sheet.

#### Scenario: Row menu shows both actions in a styled popup
- **WHEN** an operator taps the "⋮" button on a weight-detail row where `CanChangeProductMenu` is true
- **THEN** a themed popup appears listing "Cambiar producto" and "Cambiar socio" as distinct, styled options, plus a way to cancel, rendered with the app's theme colors rather than the OS's native action sheet styling
