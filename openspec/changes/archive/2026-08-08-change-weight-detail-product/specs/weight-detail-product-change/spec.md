## ADDED Requirements

### Requirement: Change the product on an existing weight detail
The system SHALL expose `PATCH /api/Weight/Detail/{id}/Product` to change `FK_WeightedProductId` and replace `ProductPrice` on an existing `WeightDetail` with the new product's current price. `Weight`, `Tare`, `SecondaryTare`, `RequiredAmount`, `Costales`, `Notes`, `WeightedBy`, and `IsLoaded` on the detail SHALL remain unchanged. The endpoint SHALL apply regardless of how much weight or required amount has already been captured on the detail.

#### Scenario: Successfully change product on a detail with no weight captured yet
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Product` with a valid `NewProductId` and the correct `PasswordHash`, for a detail with `Weight == 0`
- **THEN** `detail.FK_WeightedProductId` is set to `NewProductId`, `detail.ProductPrice` is set to the new product's current price, all other detail fields are unchanged, and the server returns `200 OK`

#### Scenario: Successfully change product on a detail with weight already captured
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Product` with a valid `NewProductId` and the correct `PasswordHash`, for a detail with `Weight > 0`
- **THEN** `detail.FK_WeightedProductId` and `detail.ProductPrice` are updated; `Weight`, `Tare`, and `WeightedBy` are unchanged; the server returns `200 OK`

### Requirement: Password gate on product change
The system SHALL require a `PasswordHash` in the request body, compared against a single shared password hash configured in application settings. The client SHALL be responsible for hashing the operator-entered plaintext password before sending it; the server SHALL never receive or need the plaintext.

#### Scenario: Reject on incorrect password
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Product` with a `PasswordHash` that does not match the configured hash
- **THEN** the server returns `400 Bad Request` with a message indicating an incorrect password, and no fields on the detail are changed

### Requirement: Credit re-validation on product change
The system SHALL re-validate the parent `WeightEntry`'s partner credit using the existing partner-credit validation whenever the new product's price would increase the detail's cost. The `requestedAmount` passed to that validation SHALL be the incremental increase only — `(newProduct.Precio - detail.ProductPrice) * quantity`, where `quantity` is `Weight` if captured (`> 0`) else `RequiredAmount` — never the full new cost, to avoid double-counting the detail's existing cost already included in the partner's pending-entries total. If the new price is equal to or lower than the current price, credit SHALL NOT be re-validated.

#### Scenario: Reject when the price increase exceeds available credit
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Product` with the correct password, and the new product's price increase (times quantity) would make the partner's credit validation fail
- **THEN** the server returns `400 Bad Request` with a message indicating insufficient credit, and no fields on the detail are changed

#### Scenario: Allow a same-or-lower-priced product without a credit check
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Product` with the correct password, and the new product's price is less than or equal to the detail's current `ProductPrice`
- **THEN** the change is applied without calling the credit validation, and the server returns `200 OK`

### Requirement: Applies even on a concluded weight entry
Unlike every other `WeightDetail` mutation endpoint, `PATCH /api/Weight/Detail/{id}/Product` SHALL NOT reject the request when the parent `WeightEntry.ConcludeDate` is set. The password check is the intended authorization to correct a product after conclusion.

#### Scenario: Successfully change product on a detail belonging to a concluded entry
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Product` with the correct password, for a detail whose parent `WeightEntry` has `ConcludeDate != null`
- **THEN** the change is applied and the server returns `200 OK`

### Requirement: Concurrency protection consistent with other detail mutations
The endpoint SHALL use the same optimistic-concurrency handling as the other `Detail/{id}/...` endpoints: a concurrent modification of the same detail SHALL result in `409 Conflict`.

#### Scenario: Concurrent modification returns a conflict
- **WHEN** two terminals attempt to mutate the same `WeightDetail` concurrently, one via `PATCH .../Product` and the resulting write loses the optimistic-concurrency check
- **THEN** the server returns `409 Conflict` with a message indicating the record was modified by another terminal
