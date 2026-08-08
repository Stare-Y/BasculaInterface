## 1. Backend — Configuration

- [x] 1.1 Add `ChangeProductPasswordHash` (string) to `WeightSettings` (`Core.Application/Settings/WeightSettings.cs`)
- [x] 1.2 Add `ChangeProductPasswordHash` value to `appsettings.json` (left empty by default — an empty configured hash disables the feature until an operator sets a real SHA-256 hash; `appsettings.Development.json` has no `WeightSettings` section to override, confirmed)

## 2. Backend — Service Layer

- [x] 2.1 Add `Task ChangeDetailProductAsync(int detailId, int newProductId, string passwordHash)` to `IWeightService`
- [x] 2.2 Implement in `WeightService`: compare `passwordHash` (ordinal, case-insensitive) against `_weightSettings.ChangeProductPasswordHash`; throw `UnauthorizedAccessException` on mismatch (also rejects when the configured hash is empty/unset) ⚠️ HIGH-RISK: first auth-adjacent check in the codebase — keep the comparison exact, no partial matches
- [x] 2.3 Load the detail via `_weightRepo.GetDetailByIdAsync(detailId)` and the new product via `_productService.GetByIdAsync(newProductId)`; let their existing not-found behavior propagate
- [x] 2.4 Compute `quantity = detail.Weight > 0 ? detail.Weight : (detail.RequiredAmount ?? 0)`, `oldCost = (detail.ProductPrice ?? 0) * quantity`, `newCost = newProduct.Precio * quantity`
- [x] 2.5 If `newCost > oldCost`, call `ValidatePartnerCreditAsync(detail.WeightEntry.PartnerId ?? 0, newCost - oldCost)`; if `!result.IsValid`, throw an exception carrying `result.Message` so the controller returns `400 Bad Request` without persisting anything ⚠️ HIGH-RISK: must not double-count against `ValidatePartnerCreditAsync`'s own `pendingEntriesCost` sum — pass the delta, not the full `newCost` (see design.md Decision 4)
- [x] 2.6 Set `detail.FK_WeightedProductId = newProductId` and `detail.ProductPrice = newProduct.Precio`; call `_weightRepo.UpdateDetailAsync(detail)` — do **not** check `WeightEntry.ConcludeDate` and do **not** call `RecomputeBruteWeightAsync` ⚠️ HIGH-RISK: intentionally bypasses the concluded-entry lock every other detail mutation enforces — confirmed as a product decision, not an oversight
- [x] 2.7 _(discovered during implementation)_ `WeightRepo.UpdateDetailAsync` re-fetches its own tracked entity and only copies a fixed field whitelist (`Weight`, `Tare`, `SecondaryTare`, `WeightedBy`, `IsLoaded`) — it silently dropped `FK_WeightedProductId`/`ProductPrice` changes. Extended the whitelist to include both fields; safe for existing callers (`SetSecondaryTareAsync`/`RecordWeightAsync`) since they load-then-mutate the same tracked instance, so those two fields round-trip unchanged for them ⚠️ HIGH-RISK: touches the shared detail-update path

## 3. Backend — Controller

- [x] 3.1 Add `public record ChangeDetailProductRequest(int NewProductId, string PasswordHash);` to `WeightController.cs`, alongside `SetSecondaryTareRequest`/`RecordWeightRequest`
- [x] 3.2 Add `[HttpPatch("Detail/{id}/Product")]` action calling `_weightService.ChangeDetailProductAsync`; map `WeightConcurrencyException` → `409 Conflict`, `UnauthorizedAccessException` → `400 Bad Request` ("Contraseña incorrecta."), other exceptions → `400 Bad Request` with `ex.Message` (matching the pattern of the other `Detail/{id}/...` actions). Verified: `dotnet build BasculaTerminalApi` succeeds, 0 warnings/errors.

## 4. MAUI — Password Hashing Helper

- [x] 4.1 Added `BasculaInterface/Services/PasswordHasher.cs` — static `HashSha256Hex(string)` helper (lowercase hex), used to hash the operator's password input before sending. Kept out of the shared `ApiService`/`Infrastructure` project since hashing plaintext is a client-only concern.

## 5. MAUI — Confirmation Popup

- [x] 5.1 Create `Views/PopUps/ChangeProductConfirmPopUp.xaml(.cs)` modeled on `PickQuantityPopUp`/`PickNotesPopUp`: `ContentView` + `TaskCompletionSource<...>`, showing the selected product's name and a password `Entry`, with Confirm/Cancel buttons

## 6. MAUI — DetailedWeightView Row Menu

- [x] 6.1 Added a "⋮" (`BtnRowMenu`) button to the weight-detail row template in `DetailedWeightView.xaml`. Note: this codebase's only existing "hover" mechanism is `VisualStateManager` `PointerOver` (background/stroke only) which MAUI cannot use to toggle a _different_ element's visibility (no `Setter.TargetName` cross-element support in MAUI's VSM). Used `PointerGestureRecognizer` `PointerEntered`/`PointerExited` on the row `Border` instead, with code-behind toggling `BtnRowMenu.IsVisible` via `Element.FindByName<Button>`. On Android (no hover concept), the button defaults to always-visible via `OnPlatform`; pointer events simply never fire there so nothing overrides that default.
- [x] 6.2 Wired the "⋮" tap to a `DisplayActionSheet` with a single "Cambiar producto" option (simplest idiomatic MAUI menu for one action; no existing flyout-menu convention in this codebase to match). Confirming opens `ProductSelectView`/subscribes to `OnProductSelected` (same pattern `BtnNuevoProducto_Clicked` uses), then shows `ChangeProductConfirmPopUp` for the selected row's detail before calling `DetailedWeightViewModel.ChangeDetailProductAsync`.
- [x] 6.3 _(new)_ Restricted the "⋮" menu to the main terminal, per project owner's explicit follow-up requirement. Added `WeightEntryDetailRow.CanChangeProductMenu` (mirrors the existing `IsSecondaryTerminal` computed-property style already on this class) and a new `RowMenuVisibilityConverter` that ANDs it with the existing per-platform baseline (`OnPlatform Android=True, WinUI=False`) so the gate holds on both platforms without disturbing the WinUI hover-to-reveal UX when allowed. Also updated `DetailRow_PointerEntered` to set visibility from `row.CanChangeProductMenu` instead of unconditionally `true` (the old code would have popped the button back open on hover even when gated off), and added a defense-in-depth early-return in `RowMenu_Clicked`.
  - Rule (corrected after initial pass): allowed whenever `!SecondaryTerminal && !OnlyFinished` — i.e. both "Solo Pedidos" mode _and_ the default/"main" mode (no terminal mode checked at all) show the menu; only "terminal secundaria" and "OnlyFinished"/"Solo Concluidos" hide it. `OnlyPedidos` itself is not part of the condition.

## 7. MAUI — DetailedWeightViewModel

- [x] 7.1 Added `ChangeDetailProductAsync(int detailId, int newProductId, string passwordPlaintext)`: hashes the password via `PasswordHasher`, calls `PATCH api/Weight/Detail/{id}/Product` via `IApiService.PatchAsync`, calls `FetchNewWeightDetails()` to refresh rows on success. Throws (does not catch) on failure — matches the existing `ChangeTargetDocumentBehavior`/`AddProductToWeightEntry` convention in this ViewModel, where the code-behind is the layer that catches and calls `DisplayAlert`
- [x] 7.2 On failure, the code-behind (`DetailedWeightView.xaml.cs`, see section 6) catches and shows the server's `ex.Message` via `DisplayAlert` — same pattern already used by `OnProductSelected`/`BtnNuevoProducto_Clicked` in this file; the API's `GenericResponse.Message` distinguishes wrong-password / insufficient-credit / concurrency-conflict cases

## 9. Credit Validation Consistency (discovered while testing this change)

- [x] 9.1 Fixed a semantic bug in `WeightService.ValidatePartnerCreditAsync` (`Infrastructure/Service/WeightService.cs`): `CreditLimit <= 0` was treated as "can't buy on credit" (hard deny), which is backwards — confirmed with the system's domain expert that `CreditLimit == 0` means **unlimited** credit, same as `IgnoreCreditLimit`. Merged both conditions into one always-valid branch. This is what caused "el socio no tiene ningún límite de crédito configurado" to surface when changing a product.
- [x] 9.2 Added the missing credit gate to the "classic"/weightless entry flow (`BasculaViewModel.CaptureNewWeightEntry`, `BasculaInterface/ViewModels/BasculaViewModel.cs`) — this flow previously posted a partner + product with **no credit check at all**, unlike "Add Product" and "Change Product" in `DetailedWeightView`. New private `ValidateCreditForCaptureAsync()` is called right after the existing `ValidateBeforePosting()` at the top of `CaptureNewWeightEntry`, before any POST happens (so nothing is created if credit is denied). Mirrors the same skip conditions (`IgnoreCreditLimit || CreditLimit <= 0` ⇒ unlimited, skip the network round-trip) as the other two flows, then defers to `api/Weight/ValidateCredit` for the real check.
- [x] 9.3 Audited `DetailedWeightView.xaml.cs`'s existing "Add Product" local pre-check — no code change needed there; once 9.1 landed, its existing `IgnoreCreditLimit || CreditLimit <= 0` skip condition became correct instead of silently contradicting the server.
- [x] 9.4 Manual test: partner with `CreditLimit == 0` can now capture a classic weight entry, add a product, and change a product — all without a credit error (previously only "Add Product" allowed this, by accident).
- [x] 9.5 Manual test: partner with a real `CreditLimit > 0` and insufficient remaining credit is still correctly blocked in all three flows (classic capture, Add Product, Change Product).
- [x] 9.6 _(found during emulator testing)_ Fixed stale row after a successful product change: `DetailedWeightViewModel.FetchNewWeightDetails` only did a full row reload (`LoadProductsAsync`, which is what refreshes `Description`/`ProductPrice`) when the detail _count_ changed; a same-count product swap took the cheap `UpdateExistingDetailRows` path, which never touched product fields, so the row kept showing the old product name until the page was left and reopened. Now also triggers the full reload when any existing row's `FK_WeightedProductId` differs from its detail's.

## 8. Verification

- [x] 8.0 `dotnet build BasculaTerminalApi/BasculaTerminalApi.csproj` succeeds — 0 warnings, 0 errors (covers `Core.Application`, `Core.Domain`, `Infrastructure`, `BasculaTerminalApi` transitively). The MAUI (`BasculaInterface`) project targets `net8.0-android`/`net8.0-windows10.0.19041.0`, and this sandbox has no MAUI workloads installed (`dotnet workload list` is empty), so it could not be built here — project owner confirmed they can build/verify it on their emulator.
- [x] 8.1 Unit/manual test: change product on a detail with `Weight == 0` (not yet captured) — succeeds, `RequiredAmount`/`Costales`/`Notes` unchanged
- [x] 8.2 Unit/manual test: change product on a detail with `Weight > 0` (already captured) — succeeds, `Weight`/`Tare`/`WeightedBy` unchanged, `ProductPrice` reflects the new product
- [x] 8.3 Manual test: change product on a detail belonging to a **concluded** `WeightEntry` — succeeds (confirms Decision 3 bypass works as intended)
- [x] 8.4 Manual test: wrong password — request is rejected, no fields on the detail change
- [x] 8.5 Manual test: new product's price increase pushes the partner over their credit limit — request is rejected with a clear message, no fields change; a price _decrease_ or unchanged price does **not** trigger a spurious credit rejection
- [x] 8.6 Manual test: concurrent modification of the same detail from two terminals — returns `409 Conflict` consistent with the other `Detail/{id}/...` endpoints
- [x] 8.7 _(new)_ Before shipping to production, set a real `ChangeProductPasswordHash` in `appsettings.json` — left empty by this change so the feature is inert until configured
- [x] 8.8 _(new)_ MAUI build/smoke test on the project owner's emulator: row hover shows "⋮" on WinUI, always visible on Android; tapping opens the action sheet → product picker → password popup → success refreshes the row
