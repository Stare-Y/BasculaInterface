## Why

Querying the audit log for a weight entry's full history showed almost no rows. Tracing every `IAuditLogService.RecordAsync` call site found the audit log only ever fires from 7 specific, gate-protected service methods (delete `WeightEntry`/`WeightDetail`, change detail product/partner/amount, delete `Pedido`/`PedidoLine`) — everything else that mutates state writes zero audit rows. This is narrower than the audit-log spec itself already claims: `openspec/specs/audit-log/spec.md`'s own "Every mutating action is recorded" requirement says each mutating action against these entities SHALL be recorded, but the implementation only ever delivered that for the 7 gated ones. This change makes the implementation match that existing promise, and extends it to `User` management, which currently has zero audit trail despite being the single most sensitive, Admin/Sudo-only capability in the system.

A related finding: `WeightService.DeleteDetailAsync` (the *ungated* detail-delete path, reachable via `DELETE /api/Weight/Detail`) is live in production — the MAUI client's "✕" button for removing an as-yet-empty detail row before any weight is captured on it. It has no gate credential and, consequently, no audit trail either. The project owner decided this should be gated like every other destructive action rather than carved out as an exception — a real client deploy alongside the API is acceptable here.

## What Changes

- **New `RecordAsync` calls**, following the existing `"Entity.Action"` naming convention, added to: `WeightService.CreateAsync` (`WeightEntry.Create`), `CreateDetailAsync` (`WeightDetail.Create`), `RecordWeightAsync` (`WeightDetail.RecordWeight`), `MarkDetailLoadedAsync` (`WeightDetail.MarkLoaded`), `ConcludeAsync` (`WeightEntry.Conclude`), `SendToContpaqiComercial` (`WeightEntry.SendToContpaqiComercial`); `PedidoService.CreateAsync` (`Pedido.Create`), `CreateLineAsync` (`PedidoLine.Create`), `CloseLineAsync` (`PedidoLine.Close`), `ConvertLineToWeightAsync` (`PedidoLine.ConvertToWeight`); `UserService.CreateAsync` (`User.Create`), `UpdateAsync` (`User.Update`), `DisableAsync` (`User.Disable`).
- **`UserService` gains a new `IAuditLogService` constructor dependency** (`WeightService`/`PedidoService` already have it — zero change needed there). DI auto-resolves it; only 2 test files (`UserServiceInactivityTimeoutTests.cs`, `UserServiceNameFieldsTests.cs`) need a substitute added.
- **`AuditLogService.RecordAsync` becomes failure-isolated**: wraps its DB write in a try/catch, logging (not throwing) on failure. Applied once, centrally, so it protects both the 7 existing call sites and every new one — a logging failure can never turn a successful mutation into a failed-looking API response.
- **The ungated `DeleteDetailAsync` service method and its `DELETE /api/Weight/Detail` route are retired.** The MAUI client's empty-row "✕" delete is rewired to call the existing gated+audited `PATCH /api/Weight/Detail/{id}/Delete` (`DeleteDetailSafelyAsync`) instead — one delete path for a `WeightDetail`, not two. This reuses the gate-credential popup pattern already built for the other 7 gated actions; no new endpoint, no new gate mechanism.

## Non-goals

- No new gate mechanism — the retired route's replacement is the *existing* `DeleteDetailSafelyAsync`/self-authorize-gate, not a new one.
- The generic `WeightService.UpdateAsync` (both overloads), `PedidoService.UpdateAsync`/`UpdateLineAsync`, and `WeightService.ChangeTargetDocumentBehavior` are **not** covered here — they're broad, internally-reused update paths not named in this change's agreed scope; flagged as a candidate for a follow-up once their call patterns are mapped separately.
- No change to the `Radiography` endpoint's shape or the two already-covered capabilities (self-authorize-gate, the 7 existing actions) beyond making their audit writes failure-isolated.
- No DB migration — `AuditLogEntry`'s existing shape (`UserId`/`Timestamp`/`Action`/`EntityType`/`EntityId`) already fits every new action.

## Impact

**Terminals affected:** every terminal, one UX change — removing an as-yet-empty detail row (the "✕" button) now prompts for a gate credential, same friction as every other destructive action, where before it was instant. Every other change here is a silent server-side addition with no UI difference.

**API/Domain:** `WeightService` (new audit calls, retired ungated delete), `PedidoService`, `UserService`, `AuditLogService` (failure isolation).

**Client (`BasculaInterface`):** `DetailedWeightViewModel`'s empty-row delete path now calls the gated endpoint and prompts for a credential — a real client change, so this deploy includes both API and client.

**Existing data:** zero impact — purely additive audit rows in an existing table; no schema change.
