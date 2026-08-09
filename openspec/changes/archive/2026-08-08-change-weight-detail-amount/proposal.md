## Why

Once a `WeightDetail`'s quantity is captured — `Weight` (scale-captured, bulk/granel products) or `RequiredAmount` (manually-entered, piece-count products) — there is no supported way to correct it. `RecordWeightAsync` can technically overwrite `Weight` again, but it hard-blocks once the parent `WeightEntry` is concluded and isn't password-gated; `RequiredAmount` has no dedicated update path at all once a detail exists (`WeightRepo.UpdateAsync` deliberately no longer touches detail fields). Operators need a fast, password-gated way to fix a mis-captured quantity mid- or post-process (issue #124), reusing the manager-override password introduced in #122.

While enriching this issue, the project owner also confirmed that #122's product-change override (`ChangeDetailProductAsync`) is missing a guard it should have had from the start: it must not be usable once the entry already has a Contpaqi ERP document, the same rule `ChangePartnerAsync` (#121) already enforces. This change retrofits that guard alongside adding the new weight/quantity override.

## What Changes

- **New endpoint** `PATCH /api/Weight/Detail/{id}/Amount` — overrides `Weight` **or** `RequiredAmount` (exactly one per request) on an existing `WeightDetail`. All other detail fields (`Tare`, `SecondaryTare`, `FK_WeightedProductId`, `ProductPrice`, `Costales`, `Notes`, `WeightedBy`, `IsLoaded`) are left untouched.
- **Password gate** — reuses the exact same shared secret from #122 (`WeightSettings.ChangeProductPasswordHash`); no new setting. Same client-hashes-then-sends-hash pattern.
- **`BruteWeight` recalculation** — when `Weight` changes on a loaded detail, `WeightEntry.BruteWeight` is recomputed via the existing `RecomputeBruteWeightAsync`, the same way `RecordWeightAsync` already does. A `RequiredAmount`-only change does not touch `BruteWeight` (it never has — `RecomputeBruteWeightAsync` only sums `Weight`).
- **Credit re-validation** — re-runs `ValidatePartnerCreditAsync` using only the incremental cost increase (mirrors `ChangeDetailProductAsync`'s delta approach) whenever the new quantity would raise the detail's cost.
- **Works on concluded entries, blocked once an ERP document exists** — like #122's product-change override, this bypasses the normal "concluded = read-only" lock, but hard-rejects once `WeightEntry.ConptaqiComercialFK > 0` (mirrors #121's `ChangePartnerAsync` rule).
- **Retrofit `ChangeDetailProductAsync` (#122)** with the same `ConptaqiComercialFK > 0` guard — it currently has none, which the project owner flagged as a gap that must be closed as part of this change.
- **Bug fix**: `WeightRepo.UpdateDetailAsync` currently never persists `RequiredAmount` on update (only `Weight`, `Tare`, `SecondaryTare`, `WeightedBy`, `IsLoaded`, `FK_WeightedProductId`, `ProductPrice`, `LastUpdated`). Without fixing this, a `RequiredAmount` override would silently no-op — the same class of bug #122 found and fixed for `FK_WeightedProductId`/`ProductPrice`.
- **MAUI client (`BasculaInterface`)**: the existing row "⋮" menu (`RowActionMenuPopUp`, introduced in #122/#121) gets a third option, "Cambiar peso/cantidad", opening a new confirmation popup (new value + password) before calling the endpoint.

## Capabilities

### New Capabilities
- `weight-detail-amount-change`: Password-gated endpoint to override a `WeightDetail`'s `Weight` or `RequiredAmount` after capture, with `BruteWeight` recomputation, incremental credit re-validation, and the same concluded-entry-bypass / ERP-document-block rule as the sibling override endpoints.

### Modified Capabilities
- `weight-detail-product-change`: Adds the missing "blocked once a Contpaqi document exists" guard to the existing `PATCH /api/Weight/Detail/{id}/Product` endpoint, aligning it with `weight-entry-partner-change`'s existing rule.

## Non-goals

- Building this flow in the React admin frontend (`BasculaUi`) — MAUI desktop (`BasculaInterface`) only, consistent with #121/#122's scope decisions.
- Any permanent/per-user authentication or authorization model — still the same #122 stopgap password.
- Changing `Tare`, `SecondaryTare`, `Costales`, `Notes`, `WeightedBy`, `FK_WeightedProductId`, `ProductPrice`, or `IsLoaded` — only `Weight`/`RequiredAmount` move.
- An audit trail beyond what already exists (`LastUpdated`) — left as an open question, same as #122.
- Renaming `ChangeProductPasswordHash` to a more generic name now that it gates three actions — cosmetic only, deferred to avoid an unnecessary production `appsettings.json` coordination step.

## Impact

**Affected terminals:** Main and "Solo Pedidos" terminals (where `DetailedWeightView`'s row menu is shown); Secondary terminal is unaffected.
**API:** `BasculaTerminalApi` — new `WeightController` action, new request record; `IWeightService`/`WeightService` new method plus a fix to the existing `ChangeDetailProductAsync`.
**Repo layer:** `WeightRepo.UpdateDetailAsync` gains `RequiredAmount` to its persisted-field whitelist.
**Client:** `BasculaInterface` — `DetailedWeightView.xaml(.cs)`, `RowActionMenuPopUp.xaml(.cs)`, `DetailedWeightViewModel.cs`, new `ChangeAmountConfirmPopUp`.
**Database:** No schema migration.
**Config:** None new — reuses `WeightSettings.ChangeProductPasswordHash`.
