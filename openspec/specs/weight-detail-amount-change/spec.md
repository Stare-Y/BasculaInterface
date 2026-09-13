# Weight Detail Amount Change — Spec

## Purpose

Defines the gated endpoint that lets an operator override the captured `Weight` or `RequiredAmount` on an existing `WeightDetail`, even after capture, with `WeightEntry.BruteWeight` recomputed when `Weight` changes and credit re-validated when the edit increases cost. This is a deliberate, explicit override — not a general-purpose edit path — intended to correct a mis-captured quantity without discarding work already captured on the detail. Shares the same gate mechanism as `weight-detail-product-change` and `weight-entry-partner-change`, and the same "blocked once an ERP document exists" boundary. Originally a single shared password, superseded by the per-user `GateIdentifier`/`GatePassword` self-authorize gate once `add-user-authentication-and-audit-log` introduced real user identity — see `self-authorize-gate`.

## Requirements

### Requirement: Change the captured amount on an existing weight detail
The system SHALL expose `PATCH /api/Weight/Detail/{id}/Amount` to override `Weight` or `RequiredAmount` on an existing `WeightDetail`. The request SHALL supply exactly one of `NewWeight` or `NewRequiredAmount`, each greater than 0. `Tare`, `SecondaryTare`, `FK_WeightedProductId`, `ProductPrice`, `Costales`, `Notes`, `WeightedBy`, and `IsLoaded` on the detail SHALL remain unchanged.

#### Scenario: Successfully override the captured Weight
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with `NewWeight > 0` and a valid `GateIdentifier`/`GatePassword` credential, for a detail with a previously captured `Weight`
- **THEN** `detail.Weight` is updated to `NewWeight`, all other detail fields are unchanged, and the server returns `200 OK`

#### Scenario: Successfully override RequiredAmount
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with `NewRequiredAmount > 0` and a valid `GateIdentifier`/`GatePassword` credential, for a detail with a previously captured `RequiredAmount`
- **THEN** `detail.RequiredAmount` is updated to `NewRequiredAmount`, all other detail fields are unchanged, and the server returns `200 OK`

#### Scenario: Reject when both NewWeight and NewRequiredAmount are supplied
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with both `NewWeight` and `NewRequiredAmount` non-null
- **THEN** the server returns `400 Bad Request` and no fields on the detail are changed

#### Scenario: Reject when neither NewWeight nor NewRequiredAmount is supplied
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with both `NewWeight` and `NewRequiredAmount` null
- **THEN** the server returns `400 Bad Request` and no fields on the detail are changed

#### Scenario: Reject a non-positive override value
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with the supplied field (`NewWeight` or `NewRequiredAmount`) `<= 0`
- **THEN** the server returns `400 Bad Request` and no fields on the detail are changed

### Requirement: Self-authorize gate on amount change
**Updated by `add-user-authentication-and-audit-log`**: the system SHALL require a `GateIdentifier`/`GatePassword` credential in the request body, resolved and verified per the `self-authorize-gate` capability.

#### Scenario: Reject an unauthorized gate credential
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with a `GateIdentifier`/`GatePassword` that fails to resolve to a user, fails password verification, or resolves to a user whose effective `CanSelfAuthorizeGate` is `false`
- **THEN** the server returns `400 Bad Request` and no fields on the detail are changed

### Requirement: BruteWeight recomputed only when Weight changes
The system SHALL recompute `WeightEntry.BruteWeight` after a successful `Weight` override, if and only if the detail is currently `IsLoaded == true` — the same condition `RecordWeightAsync` already uses. A `RequiredAmount`-only change SHALL NOT trigger a `BruteWeight` recomputation.

#### Scenario: BruteWeight recomputed after a Weight override on a loaded detail
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with `NewWeight` and the correct password, for a detail with `IsLoaded == true`
- **THEN** `WeightEntry.BruteWeight` is recomputed as `TareWeight + Σ(IsLoaded details).Weight` using the new value

#### Scenario: BruteWeight unchanged after a RequiredAmount override
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with `NewRequiredAmount` and the correct password
- **THEN** `WeightEntry.BruteWeight` is not recomputed

### Requirement: Credit re-validation on amount increase
The system SHALL re-validate the parent `WeightEntry`'s partner credit whenever the requested change would increase the detail's cost (`ProductPrice * quantity`). `quantity` before the edit SHALL be `Weight` if captured (`> 0`) else `RequiredAmount`; `quantity` after the edit SHALL be the new value if `NewWeight` was supplied, else `Weight` if the detail already has `Weight > 0` (unchanged by this request) else `NewRequiredAmount`. The `requestedAmount` passed to the existing partner-credit validation SHALL be the incremental increase only (`newCost - oldCost`), never the full new cost. If the resulting cost is equal to or lower than the current cost, credit SHALL NOT be re-validated.

#### Scenario: Reject when an amount increase exceeds available credit
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with the correct password, and the new quantity's cost increase would make the partner's credit validation fail
- **THEN** the server returns `400 Bad Request` with a message indicating insufficient credit, and no fields on the detail are changed

#### Scenario: Allow a same-or-lower quantity without a credit check
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with the correct password, and the new quantity's cost is less than or equal to the detail's current cost
- **THEN** the change is applied without calling the credit validation, and the server returns `200 OK`

#### Scenario: A RequiredAmount override on a detail with an already-captured Weight does not affect cost
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with `NewRequiredAmount` and the correct password, for a detail whose `Weight > 0`
- **THEN** `detail.RequiredAmount` is updated, but the credit validation is not triggered by this request (the quantity used for cost remains `Weight`, unchanged)

### Requirement: Applies even on a concluded weight entry, blocked once an ERP document exists
`PATCH /api/Weight/Detail/{id}/Amount` SHALL NOT reject the request when the parent `WeightEntry.ConcludeDate` is set. It SHALL reject the request when `WeightEntry.ConptaqiComercialFK > 0`, regardless of `ConcludeDate`.

#### Scenario: Successfully override an amount on a detail belonging to a concluded entry
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with the correct password, for a detail whose parent `WeightEntry` has `ConcludeDate != null` and `ConptaqiComercialFK` null or 0
- **THEN** the change is applied and the server returns `200 OK`

#### Scenario: Reject when the entry already has a Contpaqi document
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Amount` with the correct password, for a detail whose parent `WeightEntry` has `ConptaqiComercialFK > 0`
- **THEN** the server returns `400 Bad Request` with a message indicating the entry already has an ERP document, and no fields on the detail are changed

### Requirement: Concurrency protection consistent with other detail mutations
The endpoint SHALL use the same optimistic-concurrency handling as the other `Detail/{id}/...` endpoints: a concurrent modification of the same detail SHALL result in `409 Conflict`.

#### Scenario: Concurrent modification returns a conflict
- **WHEN** two terminals attempt to mutate the same `WeightDetail` concurrently, one via `PATCH .../Amount`, and the resulting write loses the optimistic-concurrency check
- **THEN** the server returns `409 Conflict` with a message indicating the record was modified by another terminal

### Requirement: Row menu offers an amount-change action, gated to the main terminal
The MAUI client's row-level "⋮" menu SHALL offer a "Cambiar peso"/"Cambiar cantidad" action alongside the existing "Cambiar producto"/"Cambiar socio" actions, under the same visibility rule (`CanChangeProductMenu`: not `SecondaryTerminal`, not `OnlyFinished`).

#### Scenario: Row menu shows all three actions in the styled popup
- **WHEN** an operator taps the "⋮" button on a weight-detail row where `CanChangeProductMenu` is true
- **THEN** the themed popup lists "Cambiar producto", "Cambiar socio", and "Cambiar peso"/"Cambiar cantidad" as distinct, styled options, plus a way to cancel
