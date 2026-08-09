## 1. Backend — Cleanup Dead Scaffold

- [x] 1.1 Remove `Task TrySwapPartner(int weightId, int currentPartnerId, int newPartnerId)` from `IWeightService`
- [x] 1.2 Remove its no-op implementation from `WeightService` (`return;` stub)
- [x] 1.3 Remove the `[HttpPost("Partner/Swap")]` `SwapPartner` action from `WeightController`
- [x] 1.4 Remove the `SwapPartnerRequest` DTO (`Core.Application/DTOs/Request/SwapPartnerRequest.cs`) — also removed the now-empty `Core.Application/DTOs/Request/` directory and the controller's now-unused `using Core.Application.DTOs.Request;`
- [x] 1.5 Confirmed via grep: no remaining references to `TrySwapPartner`/`SwapPartnerRequest`/`Partner/Swap` anywhere in the solution

## 2. Backend — Service Layer

- [x] 2.1 Add `Task ChangePartnerAsync(int weightId, int newPartnerId, string passwordHash)` to `IWeightService`
- [x] 2.2 Implement in `WeightService`: compare `passwordHash` (ordinal, case-insensitive) against `_weightSettings.ChangeProductPasswordHash`; throw `UnauthorizedAccessException` on mismatch (also reject when the configured hash is empty/unset) ⚠️ HIGH-RISK: shared password now gates a second, billing-relevant action — keep the comparison exact
- [x] 2.3 Load the entry via `_weightRepo.GetByIdAsync(weightId)`; let its existing not-found (`KeyNotFoundException`) behavior propagate
- [x] 2.4 If `entry.ConptaqiComercialFK > 0`, throw `InvalidOperationException` with a message indicating the entry already has a Contpaqi document — do **not** check `ConcludeDate` (see design.md Decision 4) ⚠️ HIGH-RISK: gets the block condition backwards easily (checking conclusion instead of ERP-sent status) — confirmed explicitly with the project owner during enrichment, do not "fix" this to check `ConcludeDate` instead
- [x] 2.5 _(simplified during implementation)_ Dropped the separate `_clienteProveedorService.GetById(newPartnerId)` lookup — `ValidatePartnerCreditAsync` (called in 2.7) already fetches the new partner internally and throws its own not-found error, so a standalone lookup here was a redundant network round-trip with an unused result
- [x] 2.6 Compute `totalCost = Σ (detail.ProductPrice ?? 0) * quantity` across `entry.WeightDetails`, `quantity = detail.Weight > 0 ? detail.Weight : (detail.RequiredAmount ?? 0)`
- [x] 2.7 Call `ValidatePartnerCreditAsync(newPartnerId, totalCost)`; if `!result.IsValid`, throw an exception carrying `result.Message` so the controller returns `400 Bad Request` without persisting anything ⚠️ HIGH-RISK: must pass the entry's **full** cost, not a delta — this entry isn't yet counted in the new partner's pending-entries total (see design.md Decision 5; do not copy issue #122's delta-only logic here)
- [x] 2.8 Set `entry.PartnerId = newPartnerId`; call `await _weightRepo.UpdateAsync(entry, force: true)` ⚠️ HIGH-RISK: touches the shared `WeightEntry` update path — verified `UpdateAsync`'s field whitelist still includes `PartnerId` (`WeightRepo.cs:267`) before relying on it

## 3. Backend — Controller

- [x] 3.1 Add `public record ChangePartnerRequest(int NewPartnerId, string PasswordHash);` to `WeightController.cs`, alongside `ChangeDetailProductRequest`
- [x] 3.2 Add `[HttpPatch("{id}/Partner")]` action calling `_weightService.ChangePartnerAsync`; map `WeightConcurrencyException` → `409 Conflict`, `UnauthorizedAccessException` → `400 Bad Request` ("Contraseña incorrecta."), other exceptions → `400 Bad Request` with `ex.Message` (matching `ChangeDetailProduct`'s pattern)
- [x] 3.3 Verified: `dotnet build BasculaTerminalApi` succeeds, 0 warnings/errors (had to temporarily bypass `global.json`'s pinned `8.0.415` SDK, unavailable in this sandbox — built against the installed `8.0.129` SDK instead, then restored `global.json` unchanged)

## 4. MAUI — Row Menu Popup (replaces DisplayActionSheet)

- [x] 4.1 Created `Views/PopUps/RowActionMenuPopUp.xaml(.cs)`: `ContentView` + `TaskCompletionSource<string?>`, modeled visually on `ChangeProductConfirmPopUp` (dark overlay `Grid`, `AppThemeBinding` background, `Boton`-styled rows sized like the existing Confirm/Cancel buttons — no bespoke "list row" style invented, to stay consistent with every other button in the app). Shows the row's description as a title, then two tappable rows: "Cambiar producto" and "Cambiar socio", plus a "Cancelar" close action. Resolves the `TaskCompletionSource` with the selected action string or `null` on cancel. Also wired the same WinUI Escape-to-cancel keyboard handling the other popups have.
- [x] 4.2 Added `<components:ChangePartnerConfirmPopUp x:Name="ChangePartnerPopUp" />` and `<components:RowActionMenuPopUp x:Name="RowMenuPopUp" />` to `DetailedWeightView.xaml`'s popup overlay section (alongside `WaitPopUp`, `PickPopUp`, `NotesPopUp`, `ChangeProductPopUp`)
- [x] 4.3 In `DetailedWeightView.xaml.cs`, replaced the `DisplayActionSheet` call in `RowMenu_Clicked` with `await RowMenuPopUp.ShowAsync(row.Description)`, branching on the returned string via `switch`; kept the existing `CanChangeProductMenu` defense-in-depth check. Extracted the existing "Cambiar producto" body into `StartChangeProductFlow(row)` (unchanged behavior) so both branches read symmetrically

## 5. MAUI — Change-Partner Flow

- [x] 5.1 Created `Views/PopUps/ChangePartnerConfirmPopUp.xaml(.cs)`, directly modeled on `ChangeProductConfirmPopUp.xaml(.cs)`: partner name label, password `Entry`, Confirm/Cancel buttons, same `TaskCompletionSource` pattern
- [x] 5.2 Added `<components:ChangePartnerConfirmPopUp x:Name="ChangePartnerPopUp" />` to `DetailedWeightView.xaml` (done together with 4.2)
- [x] 5.3 Added `StartChangePartnerFlow()` + `OnPartnerSelectedForChange(ClienteProveedorDto)` in `DetailedWeightView.xaml.cs`: opens `PartnerSelectView`, subscribes `OnPartnerSelected` for this flow, then shows `ChangePartnerPopUp` with the new partner's `RazonSocial` and a password field. Went with the simpler of the two options noted in this task — confirms directly against `viewModel.WeightEntry` (no `_rowPendingPartnerChange` field needed, since this action targets the whole entry, not a specific row/detail)
- [x] 5.4 Added `ChangePartnerAsync(int newPartnerId, string passwordPlaintext)` to `DetailedWeightViewModel`: hashes via `PasswordHasher.HashSha256Hex`, calls `PATCH api/Weight/{id}/Partner` via `IApiService.PatchAsync`, then calls `FetchNewWeightDetails()` — which already refreshes `Partner` whenever `WeightEntry.PartnerId` no longer matches the cached `Partner.Id`, so no extra partner-refresh logic was needed. Throws on failure, matching `ChangeDetailProductAsync`'s convention
- [x] 5.5 Wired the code-behind catch block in `OnPartnerSelectedForChange` to surface the server's error message via `DisplayAlert` (wrong password / entry already has a Contpaqi document / insufficient credit / concurrency conflict), same pattern as `OnProductSelectedForChange`

## 6. Verification

- [x] 6.1 `dotnet build BasculaTerminalApi/BasculaTerminalApi.csproj` succeeds — 0 warnings, 0 errors (see 3.3 note on the `global.json` SDK-pin workaround)
- [x] 6.2 Manual test: change partner on an entry with no details yet — succeeds (confirmed by project owner on device)
- [x] 6.3 Manual test: change partner on an entry with captured cost, new partner has enough credit — succeeds, detail fields unchanged (confirmed on device)
- [x] 6.4 Manual test: change partner where new partner's available credit is less than the entry's total cost — rejected, `PartnerId` unchanged (confirmed on device)
- [x] 6.5 Manual test: change partner on a **concluded** entry with `ConptaqiComercialFK` null/0 — succeeds (confirmed on device)
- [x] 6.6 Manual test: change partner on an entry with `ConptaqiComercialFK > 0` (has a Contpaqi document) — rejected, regardless of `ConcludeDate` (confirmed on device)
- [x] 6.7 Manual test: wrong password on either "Cambiar producto" or "Cambiar socio" — rejected, no fields change (confirmed on device)
- [x] 6.8 Manual test: concurrent modification of the same entry from two terminals — returns `409 Conflict` (confirmed on device)
- [x] 6.9 MAUI visual check: tapping "⋮" shows the new themed popup (not the native action sheet) with both actions clearly laid out, on both WinUI and Android (confirmed on device)
- [x] 6.10 Before shipping to production: confirm `ChangeProductPasswordHash` is already set in production `appsettings.json` (reused as-is; no new setting to configure) (confirmed)
- [x] 6.11 _(found during post-implementation review, fixed before archive)_ `ChangePartnerAsync` double-counted cost when the newly picked partner was the same as the entry's current partner — `ValidatePartnerCreditAsync`'s internal `pendingEntriesCost` already includes this entry when it's still tracked under that partner and not concluded, so adding `totalCost` again as `requestedAmount` overstated exposure. Fixed by short-circuiting as a no-op when `newPartnerId == entry.PartnerId`, before the password/ERP/credit checks even run.

**Note on 6.2–6.9:** this sandbox has no MAUI workloads installed (`dotnet workload list` is empty) and only the exact-pinned `8.0.415` SDK from `global.json` is targeted by the MAUI project (`net8.0-android`/`net8.0-windows10.0.19041.0`), neither available here — same limitation issue #122 hit. Verified instead by the project owner on their own device/emulator.
