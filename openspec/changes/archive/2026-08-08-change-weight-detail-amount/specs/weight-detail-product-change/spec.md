## MODIFIED Requirements

### Requirement: Applies even on a concluded weight entry, blocked once an ERP document exists
Unlike every other `WeightDetail` mutation endpoint, `PATCH /api/Weight/Detail/{id}/Product` SHALL NOT reject the request when the parent `WeightEntry.ConcludeDate` is set. The password check is the intended authorization to correct a product after conclusion. However, the endpoint SHALL reject the request when `WeightEntry.ConptaqiComercialFK > 0` (an ERP document already exists for this entry), regardless of `ConcludeDate` — the same rule `PATCH /api/Weight/{id}/Partner` already enforces.

#### Scenario: Successfully change product on a detail belonging to a concluded entry
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Product` with the correct password, for a detail whose parent `WeightEntry` has `ConcludeDate != null` and `ConptaqiComercialFK` null or 0
- **THEN** the change is applied and the server returns `200 OK`

#### Scenario: Reject when the entry already has a Contpaqi document
- **WHEN** a terminal sends `PATCH /api/Weight/Detail/{id}/Product` with the correct password, for a detail whose parent `WeightEntry` has `ConptaqiComercialFK > 0`
- **THEN** the server returns `400 Bad Request` with a message indicating the entry already has an ERP document, and no fields on the detail are changed
