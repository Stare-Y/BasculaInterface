## 1. Backend — Repo Fix (prerequisite bug)

- [x] 1.1 `WeightRepo.UpdateDetailAsync` (`Infrastructure/Repos/WeightRepo.cs`): add `existing.RequiredAmount = detail.RequiredAmount;` to the persisted-field list ⚠️ HIGH-RISK: without this, a `RequiredAmount` override silently no-ops — same bug class #122 found for `FK_WeightedProductId`/`ProductPrice`

## 2. Backend — Service Layer (new endpoint)

- [x] 2.1 Add `Task ChangeDetailAmountAsync(int detailId, double? newWeight, double? newRequiredAmount, string passwordHash)` to `IWeightService`
- [x] 2.2 Implement in `WeightService`: compare `passwordHash` (ordinal, case-insensitive) against `_weightSettings.ChangeProductPasswordHash`; throw `UnauthorizedAccessException` on mismatch or empty configured hash — same pattern as `ChangeDetailProductAsync`
- [x] 2.3 Validate exactly one of `newWeight`/`newRequiredAmount` is non-null; throw `ArgumentException` if both or neither are supplied ⚠️ HIGH-RISK: ambiguous requests must be rejected, not silently resolved
- [x] 2.4 Validate the supplied value is `> 0`; throw `ArgumentOutOfRangeException` otherwise (mirrors `RecordWeightAsync`/`SetSecondaryTareAsync`)
- [x] 2.5 Load the detail via `_weightRepo.GetDetailByIdAsync(detailId)`; let its existing not-found behavior propagate
- [x] 2.6 Throw `InvalidOperationException` if `detail.WeightEntry.ConptaqiComercialFK > 0` — do **not** check `ConcludeDate` ⚠️ HIGH-RISK: intentionally bypasses the concluded-entry lock (confirmed product decision, see design.md Decision 3), but must still respect the ERP-document boundary
- [x] 2.7 Compute `oldQuantity`/`newQuantity`/`oldCost`/`newCost` per design.md Decision 4; if `newCost > oldCost`, call `ValidatePartnerCreditAsync(detail.WeightEntry.PartnerId ?? 0, newCost - oldCost)` and throw with the validation's message if `!IsValid` ⚠️ HIGH-RISK: must pass the delta only, not the full `newCost` (avoids double-counting against `ValidatePartnerCreditAsync`'s own `pendingEntriesCost`)
- [x] 2.8 Set `detail.Weight = newWeight.Value` (if supplied) or `detail.RequiredAmount = newRequiredAmount.Value` (if supplied) — only the supplied field changes; call `_weightRepo.UpdateDetailAsync(detail)`
- [x] 2.9 If `newWeight.HasValue && detail.IsLoaded`, call `await _weightRepo.RecomputeBruteWeightAsync(detail.FK_WeightEntryId)` — skip for a `RequiredAmount`-only change

## 3. Backend — Retrofit ChangeDetailProductAsync (#122)

- [x] 3.1 Added the `if (detail.WeightEntry?.ConptaqiComercialFK > 0) throw new InvalidOperationException(...)` guard to `ChangeDetailProductAsync`, right after loading the detail and before the credit/product lookup ⚠️ HIGH-RISK: amends already-shipped/archived #122 behavior — confirmed explicitly with the project owner during issue enrichment, not a spontaneous change
- [x] 3.2 `openspec/changes/change-weight-detail-amount/specs/weight-detail-product-change/spec.md` delta written during propose; will update the main `openspec/specs/weight-detail-product-change/spec.md` when this change is synced/archived

## 4. Backend — Controller

- [x] 4.1 Added `public record ChangeDetailAmountRequest(double? NewWeight, double? NewRequiredAmount, string PasswordHash);` to `WeightController.cs`, alongside the other `Detail/{id}/...` request records
- [x] 4.2 Added `[HttpPatch("Detail/{id}/Amount")]` action calling `_weightService.ChangeDetailAmountAsync`; maps `WeightConcurrencyException` → `409 Conflict`, `UnauthorizedAccessException` → `400 Bad Request` ("Contraseña incorrecta."), other exceptions → `400 Bad Request` with `ex.Message` (matches `ChangeDetailProduct`'s catch pattern exactly)
- [x] 4.3 Could not be run in this sandbox (SDK version mismatch — see design.md/history), but the project owner confirmed `dotnet build BasculaTerminalApi/BasculaTerminalApi.csproj` succeeds on their machine.

## 5. MAUI — Row Menu (extend existing #122/#121 popup)

- [x] 5.1 `Views/PopUps/RowActionMenuPopUp.xaml`: added a fourth grid row with `btnChangeAmount` (label set dynamically in code-behind based on `isGranel` — "Cambiar peso" vs. "Cambiar cantidad")
- [x] 5.2 `Views/PopUps/RowActionMenuPopUp.xaml.cs`: added `OnChangeAmountClicked` → `CloseWithResult("Cambiar peso")`, and extended `ShowAsync(string title, bool isGranel = true)` to set the button label — following `OnChangeProductClicked`/`OnChangePartnerClicked`'s exact pattern

## 6. MAUI — Confirmation Popup (new)

- [x] 6.1 Created `Views/PopUps/ChangeAmountConfirmPopUp.xaml(.cs)`, modeled on `ChangeProductConfirmPopUp`: shows the row's current value, a numeric `Entry` for the new value, a password `Entry`, Confirm/Cancel — `ContentView` + `TaskCompletionSource<(double, string)?>` pattern (extended from the single-string pattern since this popup returns two values). Registered as `ChangeAmountPopUp` in `DetailedWeightView.xaml`.

## 7. MAUI — DetailedWeightView

- [x] 7.1 Extended `RowMenu_Clicked`'s switch in `DetailedWeightView.xaml.cs` with a `"Cambiar peso"` case calling a new `StartChangeAmountFlow(row)`; also passed `row.IsGranel` into `RowMenuPopUp.ShowAsync` so the menu button label matches the row's product type
- [x] 7.2 `StartChangeAmountFlow`: shows `ChangeAmountConfirmPopUp` directly (no picker step, unlike product/partner), then calls `DetailedWeightViewModel.ChangeDetailAmountAsync`; on failure, `DisplayAlert` with `ex.Message` (matches `OnProductSelectedForChange`/`OnPartnerSelectedForChange`'s try/catch/finally shape)

## 8. MAUI — DetailedWeightViewModel

- [x] 8.1 Added `ChangeDetailAmountAsync(int detailId, bool isGranel, double newValue, string passwordPlaintext)`: hashes the password via the existing `PasswordHasher`, sends `NewWeight` (if `isGranel`) or `NewRequiredAmount` (if not) via `PATCH api/Weight/Detail/{id}/Amount`, then calls `FetchNewWeightDetails()` to refresh (picks up recomputed `BruteWeight`/`TotalWeight` automatically). Throws on failure — matches `ChangeDetailProductAsync`/`ChangePartnerAsync`'s convention

## 9. Verification

- [x] 9.1 Manual/unit test: override `Weight` on a detail with `IsLoaded == true` — `BruteWeight` recomputed; `Tare`, `WeightedBy`, `FK_WeightedProductId`, `ProductPrice` unchanged
- [x] 9.2 Manual/unit test: override `Weight` on a detail with `IsLoaded == false` — `Weight` updated; `BruteWeight` unchanged
- [x] 9.3 Manual/unit test: override `RequiredAmount` on a non-granel detail — `RequiredAmount` updated; `BruteWeight` unchanged
- [x] 9.4 Manual/unit test: request with both `NewWeight` and `NewRequiredAmount` set — rejected, no changes persisted
- [x] 9.5 Manual/unit test: request with neither field set — rejected, no changes persisted
- [x] 9.6 Manual/unit test: request with a non-positive value — rejected, no changes persisted
- [x] 9.7 Manual test: override on a detail belonging to a **concluded but not-yet-sent-to-Contpaqi** entry — succeeds
- [x] 9.8 Manual test: override on a detail belonging to an entry with `ConptaqiComercialFK > 0` — rejected
- [x] 9.9 Manual test: wrong password — rejected, no fields change
- [x] 9.10 Manual test: amount increase that pushes the partner over their credit limit — rejected with a clear message; a decrease or unchanged cost does not trigger a spurious rejection
- [x] 9.11 Manual test: `RequiredAmount` override on a detail that already has `Weight > 0` — `RequiredAmount` updates, but no credit re-check is triggered (documented edge case, design.md Decision 4)
- [x] 9.12 Manual test: concurrent modification of the same detail from two terminals — returns `409 Conflict`
- [x] 9.13 **Retrofit regression test**: `ChangeDetailProductAsync` (#122) now rejects a product change once `ConptaqiComercialFK > 0` — previously this succeeded; confirmed it's the *only* newly-rejected case (still succeeds on a concluded-but-undocumented entry, same as before)
- [x] 9.14 MAUI build/smoke test on the project owner's emulator: "⋮" menu shows all three actions; "Cambiar peso"/"Cambiar cantidad" opens the confirmation popup → success refreshes the row and the total weight display

All items in this section were verified by the project owner on their build/emulator environment (build in this sandbox is blocked by an unrelated SDK-version mismatch — see 4.3).
