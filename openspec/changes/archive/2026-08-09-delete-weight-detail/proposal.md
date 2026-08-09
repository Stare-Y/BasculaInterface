## Why

Today a `WeightDetail` can only be removed via the "✕" button on its row, which the client only shows once the row is still fully empty (`Tare=0`, `Weight=0`, `SecondaryTare=null`, main terminal only — `DetailedWeightView.xaml:610-666`). It calls the existing `DELETE api/Weight/Detail` endpoint (`WeightController.cs:193`), which is a soft delete (`IsDeleted=true`) with no password gate and no `BruteWeight` recompute — safe today only because an empty row never contributed weight in the first place.

Once a row has captured data, there is no way to remove it. An operator who registered the wrong product on an entry has no undo path once anything has been weighed or loaded — unlike product (#122), partner (#121), and captured-amount (#124), which all already got a password-gated correction path via the row's "⋮" menu. Issue #125 asks for that same menu to gain a delete option, reusing the manager-override password and respecting the mutation-safety lesson already learned from #124: removing weight that was counted in `BruteWeight` must recompute it.

## What Changes

- **New endpoint** `PATCH /api/Weight/Detail/{id}/Delete` — soft-deletes an existing `WeightDetail` (`IsDeleted=true`), password-gated. Distinct from the existing unguarded `DELETE api/Weight/Detail?id=`, which is left untouched for the empty-row "✕" button.
- **Password gate** — reuses the exact same shared secret from #122/#121/#124 (`WeightSettings.ChangeProductPasswordHash`); no new setting.
- **`BruteWeight` recalculation** — if the deleted detail was `IsLoaded=true` (i.e. it was counted in the running total), `WeightEntry.BruteWeight` is recomputed via the existing `RecomputeBruteWeightAsync`, same as #124's `Weight`-override path. A never-loaded detail's deletion skips the recompute (nothing to subtract).
- **Works on concluded entries, blocked once an ERP document exists** — like #122/#121/#124, this bypasses the normal "concluded = read-only" lock, but hard-rejects once `WeightEntry.ConptaqiComercialFK > 0`.
- **No minimum-detail-count floor** — deleting a detail is allowed to leave its `WeightEntry` with zero remaining details; no new guard is added (confirmed with the project owner during issue enrichment).
- **No credit re-validation** — unlike product/partner/amount changes, a deletion only reduces a partner's exposure, never increases it, so `ValidatePartnerCreditAsync` is not invoked.
- **MAUI client (`BasculaInterface`)**: the existing "⋮" row menu (`RowActionMenuPopUp`) gets a fourth option, "Eliminar", opening a new confirmation popup (detail description + password, no "new value" field) before calling the endpoint.

## Capabilities

### New Capabilities
- `weight-detail-delete`: Password-gated endpoint to soft-delete a `WeightDetail` after capture, with conditional `BruteWeight` recomputation and the same concluded-entry-bypass / ERP-document-block rule as the sibling override endpoints.

## Non-goals

- Building this flow in the React admin frontend (`BasculaUi`) — MAUI desktop (`BasculaInterface`) only, consistent with #121/#122/#124's scope decisions.
- Modifying the existing unguarded `DELETE api/Weight/Detail` endpoint or the empty-row "✕" button — that path stays as-is for rows with nothing captured yet.
- Any permanent/per-user authentication or authorization model — still the #122 stopgap password.
- A minimum-detail-count floor on `WeightEntry` — explicitly decided against during issue enrichment.
- Credit re-validation — deletion never increases a partner's exposure.
- Restoring/un-deleting a soft-deleted detail — no such affordance exists for any soft-deleted entity in this codebase today.
- An audit trail beyond what already exists — left as an open question, same as #122/#121/#124.

## Impact

**Affected terminals:** Main and "Solo Pedidos" terminals (where `DetailedWeightView`'s row menu is shown); Secondary terminal is unaffected.
**API:** `BasculaTerminalApi` — new `WeightController` action, new request record; `IWeightService`/`WeightService` new method (`DeleteDetailSafelyAsync`), distinct from the existing unguarded `DeleteDetailAsync`.
**Repo layer:** No changes — reuses the existing `WeightRepo.DeleteDetailAsync` (soft delete) and `RecomputeBruteWeightAsync` as-is.
**Client:** `BasculaInterface` — `RowActionMenuPopUp.xaml(.cs)`, `DetailedWeightView.xaml.cs`, `DetailedWeightViewModel.cs`, new `DeleteDetailConfirmPopUp`.
**Database:** No schema migration.
**Config:** None new — reuses `WeightSettings.ChangeProductPasswordHash`.
