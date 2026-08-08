# Design: Weight Mutation Safety

## Context

The current API has a single `PUT /api/Weight` endpoint that accepts a full `WeightEntryDto` — including `BruteWeight`, all `WeightDetail` records with their `IsLoaded` flags and weights, and `ConcludeDate`. The `WeightRepo.UpdateAsync` method blindly overwrites all scalar fields including `BruteWeight` from whatever the client sends.

`BruteWeight` is accumulated client-side: `WeightEntry.BruteWeight += _diferenciaAbs` in `BasculaViewModel.CaptureNewWeightEntry`. When two secondary terminals operate concurrently on the same entry, each one reads a stale `BruteWeight`, computes a local increment, and the last writer wins — losing the other terminal's increment. This causes `BruteWeight` to drift below the true sum of loaded products. The divergence is variable (depends on timing) which is why it doesn't always appear.

No concurrency token exists on `WeightEntry` today.

## Goals / Non-Goals

**Goals:**
- `BruteWeight` is always the authoritative server-computed value: `TareWeight + Σ(IsLoaded details).Weight`
- Each terminal operation (set tare, record weight, mark loaded, conclude) has a dedicated endpoint that only writes what it owns
- Concurrent terminal writes on the same entry do not corrupt `BruteWeight`
- Clients recover transparently from concurrency conflicts (silent retry, no alert unless retries exhausted)
- `PUT /api/Weight` continues to work for non-integrity fields (notes, partner, plate, external target)

**Non-Goals:**
- Pessimistic locking / record reservation (overkill for this traffic level)
- Migrating existing data
- Changing the physical scale protocol
- Modifying ContpaqiComercial integration logic

## Decisions

### Decision 1: Server-side BruteWeight recomputation (over client accumulation)

**Chosen:** On every `MarkLoaded` call, the server recomputes `BruteWeight = TareWeight + Σ(IsLoaded=true details).Weight` and persists it atomically within the same transaction.

**Rejected:** Client sends the accumulated BruteWeight, server trusts it.
**Why rejected:** Client state is stale the moment another terminal writes. There is no safe way to accumulate a shared counter from multiple independent readers without a read-modify-write lock. Moving computation to the server eliminates the race entirely.

**Implication:** `detail.Tare` (snapshot of BruteWeight when that product was weighed) is also set server-side in `RecordWeight`, not sent from the client.

### Decision 2: Optimistic concurrency via PostgreSQL xmin (over pessimistic lock or no lock)

**Chosen:** `UseXminAsConcurrencyToken()` on `WeightEntry` in EF Core. PostgreSQL increments the system column `xmin` on every row write. EF Core includes it in `WHERE` clauses on UPDATE. Conflict → `DbUpdateConcurrencyException` → API returns `409 Conflict`.

**Rejected A:** Pessimistic lock (e.g., `SELECT FOR UPDATE`).
**Why rejected:** Requires holding a DB connection/transaction for the duration of a user interaction (can be seconds). Deadlock risk. Overkill for multi-second operations with small payload.

**Rejected B:** Application-level lock endpoint (extend CanWeight/ReleaseWeight pattern).
**Why rejected:** Terminals can crash or lose network between lock and release, leaving the entry permanently locked. Requires a timeout/TTL mechanism and adds a separate round-trip per operation.

**Why xmin:** Zero migration — `xmin` is a PostgreSQL system column present on every row. No new column, no new migration. EF Core has native support. The conflict window is milliseconds (the DB write itself), not seconds.

### Decision 3: Silent client retry on 409 (over surface error to user)

**Chosen:** `ApiService` catches `409 Conflict` on the narrow mutation endpoints. It waits 300ms, re-fetches `WeightEntry` from the server (fresh state), re-applies the narrow operation with the fresh payload, retries up to 3 times. Only after 3 failures does it propagate the error to the ViewModel, which shows a user-facing alert.

**Why:** A 409 on `MarkLoaded` means another terminal wrote in the last millisecond. The user pressed a button and expects it to work — they should not see "concurrent conflict" errors during normal operation. The 300ms wait is enough for the other terminal's write to settle. Refetching ensures the retry uses the correct current `BruteWeight`.

**Risk:** If a bug causes infinite conflicts (e.g., a third terminal keeps writing in between), the 3-retry limit surfaces the error rather than looping forever.

### Decision 4: Keep general PUT /api/Weight for non-integrity fields

**Chosen:** Trim `WeightRepo.UpdateAsync` (and the `PUT /api/Weight` path) to only apply: `Notes`, `VehiclePlate`, `PartnerId`, `ExternalTargetBehaviorFK`, `ContpaqiComercialFK`, `ContpaqiComercialFolio`, `RegisteredBy`. Reject attempts to set `BruteWeight`, `TareWeight`, `ConcludeDate`, or any detail weights through this path.

**Why:** Backward-compatible. The MAUI client currently calls this endpoint for notes saves and partner selection. These are safe — they don't touch weight integrity fields. Keeps the refactor scope contained.

## API Surface

```
Existing (trimmed):
  PUT /api/Weight                       → non-integrity fields only

New:
  POST /api/Weight/Detail               → add product slot; body: { FK_WeightEntryId, FK_WeightedProductId, RequiredAmount, Costales, Notes }
  PUT  /api/Weight/Detail/{id}/SecondaryTare  → body: { SecondaryTare }; sets IsLoaded=false
  PUT  /api/Weight/Detail/{id}/Weight         → body: { Weight, WeightedBy }; server sets Tare=current BruteWeight
  PUT  /api/Weight/Detail/{id}/MarkLoaded     → no body; validates Weight>0 && IsLoaded=false; sets IsLoaded=true; recomputes BruteWeight
  PUT  /api/Weight/{id}/Conclude              → validates all details loaded; sets ConcludeDate; triggers Contpaqi
```

## Service & Repo Changes

**`IWeightService` / `WeightService`:**
- Add `CreateDetailAsync(WeightDetailDto)` → calls new repo method
- Add `SetSecondaryTareAsync(int detailId, double tare)`
- Add `RecordWeightAsync(int detailId, double weight, string weightedBy)`
- Add `MarkDetailLoadedAsync(int detailId)` → recomputes BruteWeight server-side
- Add `ConcludeAsync(int weightEntryId)` → validates + sets ConcludeDate + triggers Contpaqi
- Trim `UpdateAsync(WeightEntryDto)` to reject integrity fields

**`IWeightRepo` / `WeightRepo`:**
- Add `CreateDetailAsync(WeightDetail)`
- Add `UpdateDetailAsync(WeightDetail)` — updates single detail by Id
- Add `RecomputeBruteWeightAsync(int weightEntryId)` — atomic: sums IsLoaded details, saves to WeightEntry
- Apply `UseXminAsConcurrencyToken()` to `WeightEntry` config

**`WeightDBContext` / entity config:**
- `builder.Entity<WeightEntry>().Property<uint>("xmin").IsRowVersion();` (no migration)

## MAUI Client Changes

| Current call site | New endpoint |
|---|---|
| `BasculaViewModel.PutSecondaryTara()` | `PUT /Detail/{id}/SecondaryTare` |
| `BasculaViewModel.CaptureNewWeightEntry()` (existing detail) | `PUT /Detail/{id}/Weight` |
| `BasculaViewModel.CaptureNewWeightEntry()` (new detail, tara=0) | `PUT /api/Weight` (TareWeight only) |
| `DetailedWeightViewModel.AddProductToWeightEntry()` | `POST /Detail` |
| `DetailedWeightViewModel.SetWeightDetailLoaded()` | `PUT /Detail/{id}/MarkLoaded` |
| `DetailedWeightViewModel.ConcludeWeightProcess()` | `PUT /api/Weight/{id}/Conclude` |
| `DetailedWeightViewModel.UpdateWeightEntry()` (notes/partner) | `PUT /api/Weight` (unchanged) |

`ApiService` gets a new helper: `PutWithRetryAsync<T>(string url, object? body, int maxRetries = 3)` — used for all narrow mutation endpoints. Catches `409`, waits 300ms, re-fetches WeightEntry, rebuilds request, retries.

## Risks / Trade-offs

**Risk: xmin not supported in some EF Core configurations**
→ Mitigation: `UseXminAsConcurrencyToken()` is supported in `Npgsql.EntityFrameworkCore.PostgreSQL` ≥ 6. Project already uses Npgsql EF Core 9. Verified compatible.

**Risk: Retry refetches stale data but applies wrong payload**
→ Mitigation: Each narrow endpoint computes from server state (BruteWeight recomputed server-side), so the retry payload only needs the client-supplied scalar (e.g., the same `Weight` value). No accumulation in retry logic.

**Risk: Old version of MAUI app on one terminal, new API on server**
→ Mitigation: Old endpoints remain functional for non-integrity fields. Old client calling old `PUT /api/Weight` will hit the trimmed version — weight integrity fields are silently ignored. The weight values won't be updated through the old path, which is the desired outcome. New endpoints are additive.

**Risk: ConcludeWeightProcess currently posts to Contpaqi inside the ViewModel**
→ Mitigation: Move Contpaqi trigger to `WeightService.ConcludeAsync()` server-side (it already has the dependencies). ViewModel only calls the Conclude endpoint and handles the response.

## Migration Plan

1. Deploy new API (new endpoints + trimmed PUT) — old MAUI clients continue working on non-integrity paths
2. Deploy new MAUI client — switches to narrow endpoints
3. No database migration required
4. Rollback: revert MAUI client to previous version; old `PUT /api/Weight` still accepts full DTO (kept for backward compat during transition)

## Open Questions

- None outstanding — all decisions confirmed with project owner during exploration (2026-06-21).
