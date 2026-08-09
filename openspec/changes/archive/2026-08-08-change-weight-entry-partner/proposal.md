## Why

There is currently no way to correct a `WeightEntry`'s partner (`Socio`) once it's been assigned and detail rows have captured cost — `BtnPickPartner` (the only assignment UI) hides itself the moment a partner is first set, and doing a plain `PUT /api/Weight` to change it later re-validates nothing. Separately, the "⋮" row menu added for issue #122 opens a native, unthemed `DisplayActionSheet` that won't scale well once it offers a second action (issue #121).

## What Changes

- **New service method + endpoint** — `IWeightService.ChangePartnerAsync(int weightId, int newPartnerId, string passwordHash)`, exposed as `PATCH /api/Weight/{id}/Partner`, matching the `PATCH .../Product` and `PATCH .../ChangeTargetDocumentBehavior` conventions already in `WeightController`.
- **Removes dead scaffolding** — deletes the unused `POST /api/Weight/Partner/Swap` route, `TrySwapPartner` (a no-op stub, `return;`), and `SwapPartnerRequest`. Nothing in the client calls them today.
- **Password gate** — reuses the issue-#122 pattern: request carries a `PasswordHash`; server compares against a configured SHA-256 hash (`WeightSettings.ChangeProductPasswordHash`, reused rather than duplicated — see design.md). Client hashes the operator's plaintext before sending.
- **ERP-sent guard (not conclusion)** — the endpoint rejects with `400` when `WeightEntry.ConptaqiComercialFK > 0` (a Contpaqi document already exists for this entry), regardless of `ConcludeDate`. A concluded-but-not-yet-sent entry can still have its partner changed — confirmed with the project owner, this differs from issue #122's rule (which ignores `ConcludeDate` entirely and has no ERP-sent guard).
- **Credit re-validation** — before applying the swap, re-runs `ValidatePartnerCreditAsync(newPartnerId, totalCost)`, where `totalCost` is the sum of `ProductPrice * quantity` across all of the entry's (non-deleted) details. Unlike the product-change flow (which validates only a price delta because the old cost is already counted against the *same* partner), here the *entire* entry's cost is new exposure for the incoming partner, since it isn't yet in their pending-entries total. Hard-reject (`400`) on insufficient credit — the password authorizes the identity change, not a credit override.
- **MAUI client (`BasculaInterface`)**: the row's "⋮" menu (`DetailedWeightView`) gains a "Cambiar socio" option next to "Cambiar producto", reusing the existing `PartnerSelectView`/`PartnerSelectorViewModel` picker, then a new confirmation popup (`ChangePartnerConfirmPopUp`, modeled on `ChangeProductConfirmPopUp`) with the selected partner's name and a password field.
- **Row-menu restyle** — replaces the current `DisplayActionSheet` in `RowMenu_Clicked` with a new custom popup (`RowActionMenuPopUp`) matching the app's existing popup visual language (dark overlay, theme-bound colors, rounded `Boton`-styled buttons), listing "Cambiar producto" / "Cambiar socio" as clearly organized rows instead of relying on the OS action sheet.

## Capabilities

### New Capabilities
- `weight-entry-partner-change`: Password-gated endpoint to change a `WeightEntry`'s partner with credit re-validation and an ERP-sent guard, plus the restyled row-menu UI that surfaces it alongside the existing product-change action.

### Modified Capabilities
- None. Additive to `weight-detail-product-change`'s row-menu surface; does not change that endpoint's contract.

## Non-goals

- Any change to the initial partner-assignment flow (`BtnPickPartner`) for entries with no partner yet.
- React (`BasculaUi`) implementation — MAUI (`BasculaInterface`) only, consistent with issue #122's scope.
- Any permanent/per-user authentication model.
- Reconciling an already-sent Contpaqi document (avoided by the `ConptaqiComercialFK` guard, not repaired if it somehow occurs).
- Broader visual redesign of `DetailedWeightView` beyond the "⋮" row menu.

## Impact

**Affected terminals:** Main and Pedidos-only terminals (where `DetailedWeightView`'s row menu is shown); Secondary terminal is unaffected (menu already hidden there via `CanChangeProductMenu`).
**API:** `BasculaTerminalApi` — new `WeightController` action + request record; `IWeightService`/`WeightService` new method; removes `TrySwapPartner`/`SwapPartnerRequest`/`Partner/Swap` route.
**Client:** `BasculaInterface` — `DetailedWeightView.xaml(.cs)`, `DetailedWeightViewModel.cs`, new `ChangePartnerConfirmPopUp`, new `RowActionMenuPopUp` (replaces the `DisplayActionSheet` call).
**Database:** No schema migration.
**Config:** No new setting if the existing `ChangeProductPasswordHash` is reused (see design.md Decision 2).
