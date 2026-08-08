## Why

There is currently no way to correct the product on a `WeightDetail` once it has weight, `RequiredAmount`, `Costales`, or notes captured. The only workaround is deleting and recreating the detail, which loses everything already captured and has no elevated-permission gate. Operators need a fast, quick-to-ship way to fix a miscategorized product mid- or post-process (issue #122).

## What Changes

- **New endpoint** `PATCH /api/Weight/Detail/{id}/Product` — swaps `FK_WeightedProductId` and replaces `ProductPrice` with the new product's current price. All other detail fields (`Weight`, `Tare`, `RequiredAmount`, `Costales`, `Notes`, `WeightedBy`, `IsLoaded`) are left untouched.
- **Password gate (temporary/insecure-by-design, per issue #122)** — the request DTO carries a `PasswordHash`. A single shared password's SHA-256 hash is stored in `appsettings.json` (`WeightSettings.ChangeProductPasswordHash` or a sibling setting). The MAUI client hashes the operator's plaintext input client-side before sending; the server does an exact hash comparison. No plaintext password crosses the wire or is persisted anywhere new.
- **Credit re-validation** — before applying the swap, the server re-runs the existing `ValidatePartnerCreditAsync` using the *incremental* cost created by the product change (new price − old price, floored at 0, times the detail's captured quantity). If that pushes the partner over their credit limit, the endpoint hard-rejects the change (`400 Bad Request`) — the password authorizes the product swap only, not a credit override.
- **Works on concluded entries** — unlike every other `Detail/{id}/...` mutation, this endpoint does **not** reject when the parent `WeightEntry.ConcludeDate` is set. The password prompt is the intended override for post-conclusion corrections.
- **MAUI client (`BasculaInterface`)**: a "⋮" button appears on a weight-detail row on hover, offering "Cambiar producto". It reuses the existing `ProductSelectView`/`ProductSelectorViewModel` (already used for adding products) to pick the new product, then shows a new confirmation popup (following the `PickQuantityPopUp`/`PickNotesPopUp` `ContentView` + `TaskCompletionSource` pattern) with the selected product name and a password field before calling the endpoint.

## Capabilities

### New Capabilities
- `weight-detail-product-change`: Password-gated endpoint to change the product (and re-priced) on an existing `WeightDetail`, independent of capture progress or conclusion state, with credit re-validation.

### Modified Capabilities
- None. This is additive — it does not change the contract of any existing `weight-detail-mutations` endpoint.

## Non-goals

- Implementing this flow in the React admin frontend (`BasculaUi`) — MAUI desktop (`BasculaInterface`) only, per scope decision during issue enrichment.
- Any permanent/per-user authentication or authorization model — the shared-password-hash approach is explicitly a stopgap to ship quickly (issue #122).
- Changing `RequiredAmount`, `Weight`, `Costales`, or any other captured quantity — only the product identity and its price snapshot change.
- An audit trail beyond what already exists (e.g., `LastUpdated`) — left as an explicit open question in `design.md`.

## Impact

**Affected terminals:** Primarily Main and Pedidos-only terminals (where `DetailedWeightView` is used to manage details); Secondary terminal is unaffected since it does not expose the detail row menu.
**API:** `BasculaTerminalApi` — new `WeightController` action, new request record, `IWeightService`/`WeightService` new method.
**Client:** `BasculaInterface` — `DetailedWeightView.xaml(.cs)`, `DetailedWeightViewModel.cs`, new `ChangeProductConfirmPopUp`.
**Database:** No schema migration — no new columns.
**Config:** New setting for the password hash in `appsettings.json` / `appsettings.Development.json`.
