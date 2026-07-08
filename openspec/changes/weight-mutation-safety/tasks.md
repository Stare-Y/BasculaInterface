## 1. Backend — Concurrency Token on WeightEntry

- [x] 1.1 Add `UseXminAsConcurrencyToken()` to WeightEntry entity configuration in `WeightDBContext` (no migration required — xmin is a PostgreSQL system column) ⚠️ HIGH-RISK: changes update path
- [x] 1.2 Add global or per-service catch for `DbUpdateConcurrencyException` in `WeightService`; translate to a thrown domain exception (e.g., `WeightConcurrencyException`) that the controller maps to `409 Conflict`

## 2. Backend — Repository Layer

- [x] 2.1 Add `CreateDetailAsync(WeightDetail detail)` to `IWeightRepo` and `WeightRepo` — persists a new `WeightDetail` row
- [x] 2.2 Add `UpdateDetailAsync(WeightDetail detail)` to `IWeightRepo` and `WeightRepo` — updates a single detail by `Id`
- [x] 2.3 Add `RecomputeBruteWeightAsync(int weightEntryId)` to `IWeightRepo` and `WeightRepo` — loads the entry with its details, computes `TareWeight + Σ(IsLoaded=true details).Weight`, saves to `WeightEntry.BruteWeight` atomically ⚠️ HIGH-RISK: new BruteWeight source of truth path

## 3. Backend — Service Layer

- [x] 3.1 Add `CreateDetailAsync(WeightDetailDto dto)` to `IWeightService` and `WeightService` — validates entry exists and is not concluded; calls repo `CreateDetailAsync`
- [x] 3.2 Add `SetSecondaryTareAsync(int detailId, double tare)` — validates entry not concluded, `tare > 0`; sets `SecondaryTare=tare` and `IsLoaded=false`; calls `UpdateDetailAsync` ⚠️ HIGH-RISK: IsLoaded=false path
- [x] 3.3 Add `RecordWeightAsync(int detailId, double weight, string weightedBy)` — validates entry not concluded, `weight > 0`; sets `detail.Weight=weight`, `detail.WeightedBy=weightedBy`, `detail.Tare` = current live `WeightEntry.BruteWeight` from DB; calls `UpdateDetailAsync` ⚠️ HIGH-RISK: server reads BruteWeight instead of trusting client
- [x] 3.4 Add `MarkDetailLoadedAsync(int detailId)` — validates entry not concluded, `detail.Weight > 0`, `detail.IsLoaded=false`; sets `IsLoaded=true`; calls `RecomputeBruteWeightAsync`; returns updated `WeightEntryDto` ⚠️ HIGH-RISK: authoritative BruteWeight recomputation
- [x] 3.5 Add `ConcludeAsync(int weightEntryId)` — validates entry not already concluded, all details `IsLoaded=true`, if `WeightDetails.Count > 1` then `PartnerId` must be set; sets `ConcludeDate=DateTime.UtcNow`; calls `ProviderPurchaseService.ConcludeByWeightEntryAsync`; if Contpaqi fails, log and include warning in result (do not rollback)
- [x] 3.6 Trim `WeightService.UpdateAsync` / `WeightRepo.UpdateAsync` to only apply: `VehiclePlate`, `Notes`, `PartnerId`, `ExternalTargetBehaviorFK`, `ContpaqiComercialFK`, `ContpaqiComercialFolio`, `RegisteredBy`, `TareWeight` (TareWeight is a one-time init, safe here); silently ignore `BruteWeight`, `ConcludeDate`, and detail weights ⚠️ HIGH-RISK: changes existing update path — verify existing callers still work

## 4. Backend — Controller

- [x] 4.1 Add `[HttpPost("Detail")]` action in `WeightController` → calls `WeightService.CreateDetailAsync`; returns `201 Created` with created detail
- [x] 4.2 Add `[HttpPut("Detail/{id}/SecondaryTare")]` action → calls `WeightService.SetSecondaryTareAsync`; returns `200 OK`
- [x] 4.3 Add `[HttpPut("Detail/{id}/Weight")]` action → calls `WeightService.RecordWeightAsync`; returns `200 OK`
- [x] 4.4 Add `[HttpPut("Detail/{id}/MarkLoaded")]` action → calls `WeightService.MarkDetailLoadedAsync`; returns `200 OK` with updated `WeightEntryDto`; maps `WeightConcurrencyException` to `409 Conflict`
- [x] 4.5 Add `[HttpPut("{id}/Conclude")]` action → calls `WeightService.ConcludeAsync`; returns `200 OK`; maps `WeightConcurrencyException` to `409 Conflict`
- [x] 4.6 Ensure all new actions also map `WeightConcurrencyException` to `409 Conflict` (can be done via a shared exception filter or inline in each action)

## 5. MAUI — ApiService

- [x] 5.1 Implement `PutWithRetryAsync<TResponse>(string url, object? body, Func<Task<WeightEntryDto>> refetch, Func<WeightEntryDto, object?> rebuildBody, int maxRetries = 3)` helper in `ApiService` — on `409 Conflict`: wait 300ms, call `refetch()`, call `rebuildBody()` with fresh entry, retry PUT; after 3 failures propagate the error to ViewModel for user-facing alert ("El servidor no pudo procesar la solicitud, intente de nuevo")

## 6. MAUI — BasculaViewModel

- [x] 6.1 Replace `PutSecondaryTara()` to call `PUT /api/Weight/Detail/{id}/SecondaryTare` via `PutWithRetryAsync`; remove client-side `IsLoaded=false` assignment (server now owns it) ⚠️ HIGH-RISK: IsLoaded false path
- [x] 6.2 In `CaptureNewWeightEntry()`, replace the existing-detail branch to call `PUT /api/Weight/Detail/{id}/Weight` with `{ Weight, WeightedBy }`; remove `WeightEntry.BruteWeight += _diferenciaAbs` client-side accumulation; refresh `WeightEntry` from server response ⚠️ HIGH-RISK: removes client BruteWeight accumulation
- [x] 6.3 In `CaptureNewWeightEntry()`, the new-detail / tara=0 branch can continue calling `PUT /api/Weight` but only with `TareWeight`; confirm `BruteWeight` is NOT sent in that payload

## 7. MAUI — DetailedWeightViewModel

- [x] 7.1 Replace `AddProductToWeightEntry()` to call `POST /api/Weight/Detail`; remove the current full `PutWeightEntry()` call; append returned detail to local `WeightEntry.WeightDetails` ⚠️ HIGH-RISK: changes detail creation path
- [x] 7.2 Replace `SetWeightDetailLoaded()` to call `PUT /api/Weight/Detail/{id}/MarkLoaded` via `PutWithRetryAsync`; update local `WeightEntry` from the `200 OK` response (which contains recomputed BruteWeight); remove stale `detail.Tare = WeightEntry.BruteWeight` client-side assignment ⚠️ HIGH-RISK: was the primary stale-BruteWeight overwrite site
- [x] 7.3 Replace `ConcludeWeightProcess()` to call `PUT /api/Weight/{id}/Conclude`; remove client-side validation of `IsLoaded` (server validates); handle `400 Bad Request` with message from server; Contpaqi trigger is now server-side only

## 8. Verification

- [ ] 8.1 Deploy updated API to test/production; smoke-test that existing MAUI client (old version) still works: notes updates, partner selection, and tare capture all pass through trimmed PUT without errors
- [ ] 8.2 Single-terminal end-to-end test: tare entry → add product → secondary tare → weigh product → mark loaded → conclude; verify `BruteWeight` in DB equals `TareWeight + Σ(loaded detail weights)` after each step
- [x] 8.3 Concurrent two-terminal test: have two secondary terminals weigh and mark-loaded on the same entry simultaneously; verify final `BruteWeight` equals the sum of both products (not a partial overwrite); verify silent retry triggers and no error surfaces to either user — *verified in production under supervision; not reproducible on dev PC (single machine)*
- [ ] 8.4 Deploy updated MAUI client; repeat 8.2 and 8.3 with new client to confirm full end-to-end correctness
