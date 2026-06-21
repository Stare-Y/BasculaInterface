## Why

The current `PUT /api/Weight` endpoint accepts a full `WeightEntryDto` snapshot — including `BruteWeight`, `IsLoaded` flags, and all detail weights — and blindly overwrites whatever is in the database. When multiple terminals operate concurrently on the same `WeightEntry` (secondary terminal weighing products while another sets up a new detail), each terminal's stale snapshot clobbers the other's writes. The result: `BruteWeight` drifts from the true sum of loaded products, causing the theoretical truck weight to diverge from the physical reading at conclusion. This is a production bug affecting every multi-terminal truck visit.

## What Changes

- **New narrow endpoints** replace the all-in-one PUT for weight-integrity mutations:
  - `POST /api/Weight/Detail` — add a product slot to an existing WeightEntry
  - `PUT /api/Weight/Detail/{id}/SecondaryTare` — record the empty-tarima weight; sets `IsLoaded=false`
  - `PUT /api/Weight/Detail/{id}/Weight` — record the measured net weight; server sets `detail.Tare` from live BruteWeight
  - `PUT /api/Weight/Detail/{id}/MarkLoaded` — mark detail as physically loaded; server recomputes BruteWeight
  - `PUT /api/Weight/{id}/Conclude` — conclude the entry; validates all details loaded, sets ConcludeDate, triggers Contpaqi

- **`PUT /api/Weight` (kept, scope reduced)** — only accepts non-integrity fields: `Notes`, `VehiclePlate`, `PartnerId`, `ExternalTargetBehaviorFK`. Rejects any attempt to set `BruteWeight`, `ConcludeDate`, or detail weights.

- **Server-side BruteWeight** — `BruteWeight` is never accepted from clients on integrity paths. It is recomputed atomically as `TareWeight + Σ(WeightDetails where IsLoaded=true).Weight` on every `MarkLoaded` call.

- **Optimistic concurrency with silent retry** — `WeightEntry` uses PostgreSQL `xmin` as EF Core concurrency token (no migration required). On a conflict the API returns `409 Conflict`. The MAUI client catches 409, waits 300ms, re-fetches latest state, and retries up to 3 times before surfacing an error to the user.

- **MAUI client updated** to call new endpoints from `BasculaViewModel` (`PutSecondaryTara`, `CaptureNewWeightEntry`) and `DetailedWeightViewModel` (`AddProductToWeightEntry`, `SetWeightDetailLoaded`, `ConcludeWeightProcess`).

## Capabilities

### New Capabilities
- `weight-detail-mutations`: Narrow, intent-specific API endpoints for each WeightDetail state transition (create, set secondary tare, record weight, mark loaded)
- `weight-entry-conclude`: Dedicated conclude endpoint with server-side validation and Contpaqi trigger
- `weight-concurrency`: Optimistic concurrency on WeightEntry with client-side silent retry

### Modified Capabilities
- `weight-flow`: BruteWeight computation moves to server-side; general PUT scope reduced to non-integrity fields

## Non-goals

- User authentication / per-user permissions (future)
- UI redesign on any terminal
- Changing the physical scale protocol or socket connection
- Modifying the ContpaqiComercial integration logic
- Adding WeightDetail edit/undo capability (IsLoaded is one-way)

## Impact

**Affected terminals:** All three (Main, Secondary, Pedidos-only)
**API:** `BasculaTerminalApi` — new controller actions, updated `WeightController`, updated `IWeightService`/`WeightService`/`WeightRepo`
**Client:** `BasculaInterface` — `BasculaViewModel`, `DetailedWeightViewModel`, `ApiService`
**Database:** No schema migration; `xmin` is a PostgreSQL system column already present
**Contpaqi integration:** No changes — triggered through the same service, now from the Conclude endpoint only
