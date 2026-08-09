## 1. Backend — Service Layer (new guarded method)

- [x] 1.1 Add `Task DeleteDetailSafelyAsync(int detailId, string passwordHash)` to `IWeightService`
- [x] 1.2 Implement in `WeightService`: compare `passwordHash` (ordinal, case-insensitive) against `_weightSettings.ChangeProductPasswordHash`; throw `UnauthorizedAccessException` on mismatch or empty configured hash — same pattern as `ChangeDetailProductAsync`/`ChangePartnerAsync`/`ChangeDetailAmountAsync`
- [x] 1.3 Load the detail via `_weightRepo.GetDetailByIdAsync(detailId)`; let its existing not-found behavior propagate as `404`
- [x] 1.4 Throw `InvalidOperationException` if `detail.WeightEntry?.ConptaqiComercialFK > 0` — do **not** check `ConcludeDate` ⚠️ HIGH-RISK: intentionally bypasses the concluded-entry lock (confirmed product decision, see design.md Decision 2), but must still respect the ERP-document boundary
- [x] 1.5 Capture `bool wasLoaded = detail.IsLoaded;` and `int entryId = detail.FK_WeightEntryId;` **before** deleting ⚠️ HIGH-RISK: touches the `BruteWeight` accumulation path — capturing these after the delete would read stale/wrong values
- [x] 1.6 Call `await _weightRepo.DeleteDetailAsync(detailId);` (existing, unchanged repo method)
- [x] 1.7 If `wasLoaded`, call `await _weightRepo.RecomputeBruteWeightAsync(entryId);` — skip otherwise ⚠️ HIGH-RISK: touches `BruteWeight` accumulation (design.md Decision 5); must run **after** the delete completes, not before, so the recompute's own detail query excludes the just-deleted row
- [x] 1.8 Do **not** call `ValidatePartnerCreditAsync` anywhere in this method (design.md Decision 4)

## 2. Backend — Controller

- [x] 2.1 Add `public record DeleteDetailRequest(string PasswordHash);` to `WeightController.cs`, alongside the other `Detail/{id}/...` request records ⚠️ touches the update/mutation surface of `WeightController`
- [x] 2.2 Add `[HttpPatch("Detail/{id}/Delete")]` action calling `_weightService.DeleteDetailSafelyAsync`; maps `WeightConcurrencyException` → `409 Conflict`, `UnauthorizedAccessException` → `400 Bad Request` ("Contraseña incorrecta."), other exceptions → `400 Bad Request` with `ex.Message` (matches `ChangeDetailProduct`/`ChangePartner`/`ChangeDetailAmount`'s catch pattern exactly)
- [x] 2.3 Confirmed the existing `[HttpDelete("Detail")]` action (`WeightController.cs:193-210`) is untouched — no route or behavior change
- [x] 2.4 Build `BasculaTerminalApi` and confirm no route collision between `[HttpDelete("Detail")]` and the new `[HttpPatch("Detail/{id}/Delete")]` — could not be run in this sandbox (SDK version mismatch, same blocker as `change-weight-detail-amount` task 4.3); confirmed by the project owner on their build environment: builds clean, no route collision.

## 3. MAUI — Row Menu (extend existing #122/#121/#124 popup)

- [x] 3.1 `Views/PopUps/RowActionMenuPopUp.xaml`: add a fourth grid row with `btnDelete` ("Eliminar"), styled distinctly (red background/text) from the other three buttons
- [x] 3.2 `Views/PopUps/RowActionMenuPopUp.xaml.cs`: add `OnDeleteClicked` → `CloseWithResult("Eliminar")`, following `OnChangeProductClicked`/`OnChangePartnerClicked`/`OnChangeAmountClicked`'s exact pattern

## 4. MAUI — Confirmation Popup (new)

- [x] 4.1 Created `Views/PopUps/DeleteDetailConfirmPopUp.xaml(.cs)`: shows the row's description and a password `Entry`, Confirm/Cancel — `ContentView` + `TaskCompletionSource<string?>` pattern (returns the entered password, or `null` on cancel), modeled on `ChangePartnerConfirmPopUp.ShowAsync`'s single-value return shape. Registered as `DeleteDetailPopUp` in `DetailedWeightView.xaml`.

## 5. MAUI — DetailedWeightView

- [x] 5.1 Extended `RowMenu_Clicked`'s switch in `DetailedWeightView.xaml.cs` with an `"Eliminar"` case calling a new `StartDeleteDetailFlow(row)`
- [x] 5.2 `StartDeleteDetailFlow`: shows `DeleteDetailConfirmPopUp` directly (no picker/value step, like #121's partner flow but even simpler), then calls `DetailedWeightViewModel.DeleteWeightDetailSafelyAsync`; on failure, `DisplayAlert` with `ex.Message` (matches `OnPartnerSelectedForChange`/`StartChangeAmountFlow`'s try/catch/finally shape); on success, `FetchNewWeightDetails()` (called inside the ViewModel method) refreshes the row collection and total-weight display

## 6. MAUI — DetailedWeightViewModel

- [x] 6.1 Added `DeleteWeightDetailSafelyAsync(int detailId, string passwordPlaintext)`: hashes the password via the existing `PasswordHasher`, calls `PATCH api/Weight/Detail/{id}/Delete`, then `FetchNewWeightDetails()` to refresh (picks up the recomputed `BruteWeight`/`TotalWeight` automatically when applicable). Throws on failure — matches `ChangeDetailProductAsync`/`ChangePartnerAsync`/`ChangeDetailAmountAsync`'s convention. Existing `DeleteWeightDetail`/`RemoveWeightEntryDetail` (unguarded, empty-row path) are left untouched.

## 7. Verification

- [x] 7.1 Manual/unit test: delete a detail with `IsLoaded == true` — `BruteWeight` recomputed to exclude it; other details' fields unchanged ⚠️ HIGH-RISK path (BruteWeight accumulation)
- [x] 7.2 Manual/unit test: delete a detail with `IsLoaded == false` — detail removed; `BruteWeight` unchanged
- [x] 7.3 Manual test: delete on a detail belonging to a **concluded but not-yet-sent-to-Contpaqi** entry — succeeds
- [x] 7.4 Manual test: delete on a detail belonging to an entry with `ConptaqiComercialFK > 0` — rejected, no fields change
- [x] 7.5 Manual test: wrong password — rejected, detail not deleted
- [x] 7.6 Manual test: delete the last remaining non-deleted detail on an entry — succeeds, entry left with zero details, no error
- [x] 7.7 Manual test: delete a detail whose parent's partner is already over their credit limit — succeeds (no credit check triggered)
- [x] 7.8 Manual test: concurrent modification of the same `WeightEntry` during the recompute step (two terminals) — returns `409 Conflict`
- [x] 7.9 Regression test: the existing empty-row "✕" button/endpoint still deletes with no password prompt and no behavior change
- [x] 7.10 MAUI build/smoke test: "⋮" menu shows all four actions; "Eliminar" opens the confirmation popup → success removes the row and refreshes the total weight display; cancel leaves the row untouched

All items in this section were verified by the project owner on their build/emulator environment (build in this sandbox is blocked by an unrelated SDK-version mismatch — see 2.4).
