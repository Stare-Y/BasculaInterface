## 1. Backend — WeightService (guarded WeightEntry delete)

- [x] 1.1 Replace `Task<bool> DeleteAsync(int id)` with `Task DeleteSafelyAsync(int id, string passwordHash)` on `IWeightService` (plain `Task`, not `Task<bool>` — matches `DeleteDetailSafelyAsync`'s existing signature convention; not-found now comes from `GetByIdAsync`'s `KeyNotFoundException`, see 1.3)
- [x] 1.2 Implement in `WeightService`: compare `passwordHash` (ordinal, case-insensitive) against `_weightSettings.ChangeProductPasswordHash`; throw `UnauthorizedAccessException` on mismatch or empty configured hash — same pattern as the existing guarded weight-detail mutations
- [x] 1.3 Load the entry via the existing `_weightRepo.GetByIdAsync(id)` to read `ConptaqiComercialFK` before deleting ⚠️ HIGH-RISK: touches the update path shared with other WeightEntry mutations — must not change any field besides `IsDeleted`
- [x] 1.4 Throw `InvalidOperationException` if `entry.ConptaqiComercialFK > 0` — do **not** check `ConcludeDate` (design.md Decision 2)
- [x] 1.5 Call the existing `_weightRepo.DeleteAsync(id)` (unchanged)
- [x] 1.6 Removed the old unguarded `DeleteAsync(int id)` — no callers remain after task 2

## 2. Backend — WeightController

- [x] 2.1 Added `public record DeleteWeightEntryRequest(string PasswordHash);`
- [x] 2.2 Replaced the `[HttpDelete]` `Delete` action with `[HttpPatch("{id}/Delete")]` calling `_weightService.DeleteSafelyAsync`; maps `KeyNotFoundException` → `404`, `UnauthorizedAccessException` → `400 Bad Request` ("Contraseña incorrecta."), other exceptions → `400 Bad Request` with `ex.Message`
- [x] 2.3 No route collision — `{id}/Delete`'s literal suffix is distinct from `{weightId}/ChangeTargetDocumentBehavior` and `{id}/Conclude`

## 3. Backend — PedidoService (guarded Pedido + PedidoLine delete)

- [x] 3.1 Added `IOptions<WeightSettings> weightSettingsOptions` to `PedidoService`'s constructor (design.md Decision 3); stored as `_weightSettings`
- [x] 3.2 Replaced `Task<bool> DeleteAsync(int id)` with `Task<bool> DeleteSafelyAsync(int id, string passwordHash)` on `IPedidoService`/`PedidoService`: same password check as 1.2, then calls the existing `_pedidoRepo.DeleteAsync(id)` (unchanged, cascades to `Lines` as today)
- [x] 3.3 Replaced `Task<bool> DeleteLineAsync(int id)` with `Task<bool> DeleteLineSafelyAsync(int id, string passwordHash)` on `IPedidoService`/`PedidoService`: same password check, then calls the existing `_pedidoLineRepo.DeleteAsync(id)` (unchanged)
- [x] 3.4 Removed the old unguarded `DeleteAsync(int id)`/`DeleteLineAsync(int id)` — no callers remain after task 4

## 4. Backend — PedidoController

- [x] 4.1 Added `public record DeletePedidoRequest(string PasswordHash);` and `public record DeletePedidoLineRequest(string PasswordHash);`
- [x] 4.2 Replaced the `[HttpDelete]` `Delete` action with `[HttpPatch("{id}/Delete")]` calling `_pedidoService.DeleteSafelyAsync`; maps `UnauthorizedAccessException` → 400, not-found (bool `false`) → 404, other exceptions → 400 with `ex.Message` (no `WeightConcurrencyException` catch — `Pedido` has no concurrency token)
- [x] 4.3 Replaced the `[HttpDelete("Line")]` `DeleteLine` action with `[HttpPatch("Line/{id}/Delete")]` calling `_pedidoService.DeleteLineSafelyAsync`; same exception mapping
- [x] 4.4 No route collision — `{id}/Delete` and `Line/{id}/Delete`'s literal suffixes are distinct from `{id}` (GetById), `Line/{id}/Close`, and `Line/{id}/ConvertToWeight`. `dotnet build BasculaTerminalApi/BasculaTerminalApi.csproj` succeeded, 0 warnings/errors

## 5. MAUI — DetailedWeightView / DetailedWeightViewModel

- [x] 5.1 `DetailedWeightViewModel.DeleteWeightEntry`: added a `string passwordPlaintext` parameter, hashes via `PasswordHasher.HashSha256Hex`, calls `PATCH api/Weight/{id}/Delete` via `_apiService.PatchAsync<GenericResponse<string>>` instead of `DeleteAsync`
- [x] 5.2 `DetailedWeightView.BtnDeleteEntry_Clicked`: replaced the `DisplayAlert` yes/no confirm with the existing `DeleteDetailPopUp` instance already on this page — extended `DeleteDetailConfirmPopUp` with an optional `title` parameter (default unchanged) so the same control now serves both detail- and entry-deletion with distinct wording ("Eliminar entrada de peso:" vs. "Eliminar producto:"); aborts with no call on cancel/empty password
- [x] 5.3 Wired the popup's returned password into the updated `DeleteWeightEntry` call; on failure, `DisplayAlert` with the error message, matching `StartDeleteDetailFlow`'s try/catch/finally shape

## 6. MAUI — PedidoFormView / PedidoFormViewModel / PedidoListViewModel

- [x] 6.1 `PedidoFormViewModel.DeletePedidoAsync`: added a `string passwordPlaintext` parameter, hashes it, calls the new `PATCH api/Pedido/{id}/Delete`
- [x] 6.2 `PedidoFormView.BtnDelete_Clicked`: replaced the `DisplayAlert` yes/no confirm with the same reused `DeleteDetailConfirmPopUp` control (new `DeletePedidoPopUp` instance added to `PedidoFormView.xaml`, alongside `WaitPopUp`/`ConvertPopUp`); aborts with no call on cancel/empty password
- [x] 6.3 `PedidoListViewModel.DeletePedidoAsync`: same signature change as 6.1, for consistency (no View wiring — no current caller)

## 7. Verification

- [x] 7.1 Unit test: `DeleteSafelyAsync` with correct password on an entry with no ERP document — succeeds (`WeightServiceDeleteSafelyAsyncTests.Succeeds_for_a_concluded_entry_with_no_Contpaqi_document`, plus the shared-gate `Accepts_a_correct_password...` theory case)
- [x] 7.2 Unit test: `DeleteSafelyAsync` with wrong/empty password — throws `UnauthorizedAccessException`, entry not deleted (`WeightServicePasswordGateTests`, extended with a `DeleteSafely` case in the existing `GatedAction` theory)
- [x] 7.3 Unit test: `DeleteSafelyAsync` on an entry with `ConptaqiComercialFK > 0` — throws `InvalidOperationException`, entry not deleted, even with the correct password (`WeightServiceDeleteSafelyAsyncTests.Rejects_when_the_entry_already_has_a_Contpaqi_document`) ⚠️ HIGH-RISK path (new behavior vs. today's unguarded delete) — verified
- [x] 7.4 Unit test: `DeleteSafelyAsync` on a concluded (`ConcludeDate != null`) entry with no ERP document — succeeds (`WeightServiceDeleteSafelyAsyncTests.Succeeds_for_a_concluded_entry_with_no_Contpaqi_document`)
- [x] 7.5 Integration test: `PATCH api/Weight/{id}/Delete` — 200 on correct password, 400 on wrong password, 404 on missing entry (`WeightPasswordGateHttpTests`, 3 new tests)
- [x] 7.6 Unit test (`PedidoServicePasswordGateTests`, new file): `DeleteSafelyAsync`/`DeleteLineSafelyAsync` — correct password succeeds, wrong password throws `UnauthorizedAccessException`, no repo calls made; also covers repo-reported not-found returning `false`
- [x] 7.7 Integration test: `PATCH api/Pedido/{id}/Delete` and `PATCH api/Pedido/Line/{id}/Delete` — 200/400/404 matrix (`PedidoDeletePasswordGateHttpTests`, new file, 6 tests)
- [x] 7.8 Regression test: confirm the old `DELETE api/Weight?id=`, `DELETE api/Pedido?id=`, and `DELETE api/Pedido/Line?id=` routes no longer resolve (404/405) — 3 tests, one per route, in the files above
- [x] 7.9 Regression test: the existing unguarded `DELETE api/Weight/Detail?id=` and its empty-row "✕" button are unaffected — verified by inspection (route/action untouched) and by the pre-existing `WeightPasswordGateHttpTests`/detail-flow integration tests, which still exercise `CreateDetailAsync`'s flow unchanged, continuing to pass
- [x] 7.10 MAUI build/smoke test: deleting a WeightEntry and deleting a Pedido each prompt for a password; wrong password shows an error and nothing is deleted; correct password deletes and navigates back as before — verified by the project owner on their build environment (this sandbox cannot build MAUI targets — see Verification run).

### Verification run

- `dotnet build BasculaTerminalApi/BasculaTerminalApi.csproj` and `dotnet build BasculaTerminalTest/BasculaTerminalTest.csproj` — both succeed, 0 warnings/errors (includes the fix to `PedidoServiceConvertLineToWeightTests.CreateSut()`, which needed the new `PedidoService` constructor parameter).
- Full `dotnet test BasculaTerminalTest/BasculaTerminalTest.csproj` (unit tests + integration tests against a real Podman-backed PostgreSQL container via Testcontainers): **93/94 passed**. The one failure, `BasculaTerminalTest.Live.BasculaClient.WebSocketTesting`, requires a live server at `localhost:5284` and is unrelated to this change (last touched in an unrelated prior commit, `970d733`) — same category of pre-existing, environment-only test the prior `delete-weight-detail` change's sandbox couldn't run either, except this sandbox *could* run everything else.
- MAUI (`BasculaInterface.csproj`) could not be built for either `net8.0-windows10.0.19041.0` (Windows XAML compiler needs real Windows) or `net8.0-android` (workload not restored) — matches the same class of sandbox limitation noted in `archive/2026-08-09-delete-weight-detail`'s task 2.4.
