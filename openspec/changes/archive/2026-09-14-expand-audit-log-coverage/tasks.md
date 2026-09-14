## 1. Failure isolation (do this first — every new call site depends on it being safe)

- [x] 1.1 `AuditLogService.cs`: add `ILogger<AuditLogService>` constructor dependency; wrap `RecordAsync`'s `_auditLogRepo.CreateAsync(...)` call in `try/catch`, logging the exception on failure and returning normally either way. `RecordAsync` must never throw.
- [x] 1.2 Unit test: a `RecordAsync` call whose repo throws does not propagate — assert the caller (a fake service method) completes normally and the exception was logged, not raised.

## 2. WeightService — new audit calls

- [x] 2.1 `CreateAsync` → `RecordAsync("WeightEntry.Create", nameof(WeightEntry), entry.Id)` after creation succeeds.
- [x] 2.2 `CreateDetailAsync` → `RecordAsync("WeightDetail.Create", nameof(WeightDetail), detail.Id)`.
- [x] 2.3 `RecordWeightAsync` → `RecordAsync("WeightDetail.RecordWeight", nameof(WeightDetail), detailId)`.
- [x] 2.4 `MarkDetailLoadedAsync` → `RecordAsync("WeightDetail.MarkLoaded", nameof(WeightDetail), detailId)`.
- [x] 2.5 `ConcludeAsync` → `RecordAsync("WeightEntry.Conclude", nameof(WeightEntry), weightEntryId)`.
- [x] 2.6 `SendToContpaqiComercial` → `RecordAsync("WeightEntry.SendToContpaqiComercial", nameof(WeightEntry), id)`, only on a successful result.

## 3. PedidoService — new audit calls

- [x] 3.1 `CreateAsync` → `RecordAsync("Pedido.Create", nameof(Pedido), dto.Id)`.
- [x] 3.2 `CreateLineAsync` → `RecordAsync("PedidoLine.Create", nameof(PedidoLine), line.Id)`.
- [x] 3.3 `CloseLineAsync` → `RecordAsync("PedidoLine.Close", nameof(PedidoLine), lineId)`.
- [x] 3.4 `ConvertLineToWeightAsync` → `RecordAsync("PedidoLine.ConvertToWeight", nameof(PedidoLine), lineId)` (in addition to whatever `WeightEntry.Create` row the internal weight-entry creation already produces per design.md Decision 5 — do not suppress that second row).
  - Note: `ConvertLineToWeightAsync` calls `IWeightRepo` directly, not `WeightService`, so the `WeightEntry.Create`/`WeightDetail.Create` row is NOT automatically produced as design.md assumed — added explicitly here in both branches to fulfill the design's stated intent.

## 4. UserService — new dependency + audit calls

- [x] 4.1 Add `IAuditLogService auditLogService` to `UserService`'s constructor.
- [x] 4.2 Update `UserServiceInactivityTimeoutTests.cs` and `UserServiceNameFieldsTests.cs`: add `Substitute.For<IAuditLogService>()` to their `CreateSut()` calls.
- [x] 4.3 `CreateAsync` → `RecordAsync("User.Create", nameof(User), created.Id)`.
- [x] 4.4 `UpdateAsync` → `RecordAsync("User.Update", nameof(User), id)`.
- [x] 4.5 `DisableAsync` → `RecordAsync("User.Disable", nameof(User), id)`.

## 5. Retire the ungated detail-delete path

- [x] 5.1 Remove `WeightService.DeleteDetailAsync` and its `[HttpDelete("Detail")]` controller action (`WeightController.cs`). Keep `IWeightRepo.DeleteDetailAsync`/`WeightRepo.DeleteDetailAsync` — still used internally by `DeleteDetailSafelyAsync`.
- [x] 5.2 Client: rewire `DetailedWeightView.xaml.cs`'s `DeleteWeightDetail_Clicked` to follow `StartDeleteDetailFlow`'s shape — `DeleteDetailPopUp.ShowAsync(row.Description)` for a gate credential, then `viewModel.DeleteWeightDetailSafelyAsync(row.Id, identifier, password)` — while preserving its existing post-success behavior (the "Éxito" alert, `BtnFinishWeight.IsVisible` recompute).
- [x] 5.3 Remove the now-dead `DetailedWeightViewModel.DeleteWeightDetail`/`RemoveWeightEntryDetail`.
- [x] 5.4 Confirm via grep: zero remaining calls to `DELETE api/Weight/Detail?id=` anywhere in the client.

## 6. Tests & verification

- [x] 6.1 New unit tests asserting each of the 13 new call sites (§2–4) writes the expected `Action`/`EntityType`/`EntityId` — mirroring the existing `WeightServiceDeleteSafelyAsyncTests.cs`/`PedidoServicePasswordGateTests.cs` pattern (assert against the `IAuditLogService` substitute's received call).
- [x] 6.2 Confirm a rejected/failed action (e.g. `CreateAsync` given invalid data, if it can fail validation) still writes no audit row — same invariant the spec already states for the 7 existing actions.
- [x] 6.3 Run the full non-integration/non-Live suite; confirm no regressions against this repo's running baseline.
  - 164/164 unit tests pass. (The `Category=Integration` suite needs Docker/testcontainers, unavailable in this sandbox — a pre-existing environment limitation, not exercised here.)
- [ ] 6.4 On-device (owner-gated, same limitation as every prior client change): confirm the "✕" empty-row delete now prompts for a gate credential and behaves identically to the existing loaded-row delete flow on success/cancel/wrong-credential.
- [ ] 6.5 On-device: spot-check the radiography query for a fresh weight entry now shows `WeightEntry.Create`, `WeightDetail.Create`, `WeightDetail.RecordWeight`, and `WeightEntry.Conclude` rows across its lifecycle.

## 7. Spec sync

- [x] 7.1 Update `openspec/specs/audit-log/spec.md`: broaden "Every mutating action is recorded" to explicitly list `User` alongside `WeightEntry`/`WeightDetail`/`Pedido`/`PedidoLine`, and add the failure-isolation requirement (Decision 1) plus a scenario confirming the retired ungated delete path no longer exists.
