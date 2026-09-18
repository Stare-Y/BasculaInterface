# Design: Expand Audit Log Coverage

## Context

Querying the audit log for a weight entry's radiography showed almost no rows. Tracing every `IAuditLogService.RecordAsync` call site found the audit trail was scoped to exactly the 7 self-authorize-gate-protected actions from the start — narrower than `openspec/specs/audit-log/spec.md`'s own "Every mutating action is recorded" requirement already claims. Investigating also surfaced a genuinely ungated delete path (`DeleteDetailAsync`, the empty-row "✕" button) with neither a gate nor an audit trail; the project owner chose to gate it rather than carve it out, accepting a client deploy.

## Goals / Non-Goals

**Goals:** cover the weighing-flow lifecycle (create entry, add detail, capture weight, mark loaded, conclude, ERP submission), the Pedido lifecycle (create, create line, close line, convert to weight), and User management (create, update, disable) with audit entries; make an audit-write failure unable to affect the action it's attached to; close the one delete path that has neither a gate nor an audit trail.

**Non-Goals:** the generic `WeightService.UpdateAsync`/`PedidoService.UpdateAsync`/`UpdateLineAsync`/`ChangeTargetDocumentBehavior` paths (broad, internally-reused, not part of the agreed scope); any change to the `Radiography` endpoint's shape; any new gate mechanism.

## Decisions

### Decision 1: Failure-isolate `AuditLogService.RecordAsync` centrally, not per call site

**Chosen:** wrap the `_auditLogRepo.CreateAsync(...)` call inside `AuditLogService.RecordAsync` itself in a `try/catch`, logging via an injected `ILogger<AuditLogService>` on failure and returning normally either way — `RecordAsync` becomes a method that cannot throw. This automatically protects the 7 existing call sites and every new one added by this change, with a one-file edit instead of wrapping ~13 new call sites individually.

**Why this matters:** every existing call site places `RecordAsync` *after* the real mutation already committed (e.g. `_weightRepo.DeleteAsync(id)` runs, then the audit call). Today, if the audit write throws, that exception propagates and the caller sees a `500` for an action that actually succeeded. That's a pre-existing risk on the 7 call sites; multiplying the call sites ~3x without fixing it would multiply the risk of an unrelated audit-table hiccup (connection pool exhaustion, a transient DB blip) breaking core weighing-floor actions that have nothing to do with auditing. This is the concrete mechanism that backs "no impact on current behavior," not just a claim.

**Rejected — wrap each new call site in its own `try/catch`:** works, but leaves the 7 existing sites still exposed and repeats the same boilerplate ~13 times for no benefit over fixing it once at the source.

### Decision 2: `UserService` takes `IAuditLogService` as a new constructor dependency

**Chosen:** add `IAuditLogService auditLogService` to `UserService`'s constructor (`WeightService`/`PedidoService` already have it — confirmed via their constructors, zero change needed there). DI auto-resolves it (`ConfigureServices.cs` uses plain `AddScoped<TInterface, TImpl>`, no explicit `new` call to update). Only `UserServiceInactivityTimeoutTests.cs` and `UserServiceNameFieldsTests.cs` construct `UserService` directly and need a `Substitute.For<IAuditLogService>()` added to their `CreateSut()`.

**Rejected — resolve the audit call via a static/service-locator pattern to avoid touching the constructor:** this codebase already has an established DI-first convention for every other service; a locator here would be inconsistent for no real savings (the blast radius is already only 2 test files).

### Decision 3: Retire the ungated `DeleteDetailAsync`, route the empty-row delete through the existing safe path

**Chosen:** remove `WeightService.DeleteDetailAsync` (the non-`async` pass-through) and the `[HttpDelete("Detail")]` controller action entirely. `WeightRepo.DeleteDetailAsync` (the repo-level method) stays — it's already reused internally by `DeleteDetailSafelyAsync` (`WeightService.cs:616`). Client-side, `DetailedWeightView.xaml.cs`'s `DeleteWeightDetail_Clicked` (bound to the "✕" button) is rewired to follow the same shape as the already-existing `StartDeleteDetailFlow` (used elsewhere for loaded-row deletion): show `DeleteDetailPopUp.ShowAsync(row.Description)` for a gate credential, then call the already-existing `DetailedWeightViewModel.DeleteWeightDetailSafelyAsync(detailId, identifier, password)` — a method and popup that already exist and are already used for the sibling flow, so this is wiring, not new plumbing. `DeleteWeightDetail_Clicked`'s existing post-success behavior (the "Éxito" alert, recomputing `BtnFinishWeight.IsVisible`) is preserved; only the pre-delete step (credential prompt) and the call target change. `DetailedWeightViewModel.DeleteWeightDetail`/`RemoveWeightEntryDetail` (the ungated VM methods) are removed as now-dead code.

**Why not add a *new*, third gated variant instead:** `DeleteDetailSafelyAsync` already is exactly "delete a WeightDetail behind the gate, audited." Two gated paths doing the same thing would be pure duplication; the fix is pointing the one remaining caller at the one that already exists.

**Consequence, stated plainly:** removing an as-yet-empty detail row now costs the same credential-prompt friction as every other destructive action. Before this change it was instant. This is the one user-facing behavior change in the whole proposal — everything else is a silent server-side addition.

### Decision 4: Action-name conventions for new call sites

**Chosen:** follow the existing `"Entity.Method"` string convention exactly (`WeightEntry.Create`, `WeightDetail.Create`, `WeightDetail.RecordWeight`, `WeightDetail.MarkLoaded`, `WeightEntry.Conclude`, `WeightEntry.SendToContpaqiComercial`, `Pedido.Create`, `PedidoLine.Create`, `PedidoLine.Close`, `PedidoLine.ConvertToWeight`, `User.Create`, `User.Update`, `User.Disable`). Each call happens only after its mutation succeeds, matching the existing "a rejected action is not recorded as successful" invariant (`audit-log/spec.md`'s own requirement) — no call is moved earlier than the point where the 7 existing ones already sit relative to their own mutations.

### Decision 5: `ConvertLineToWeightAsync` may produce two audit rows, and that's correct

**Chosen:** `PedidoLine.ConvertToWeight` is recorded against the `PedidoLine`. If that method internally creates a new `WeightEntry` via the same code path `WeightService.CreateAsync` uses, a `WeightEntry.Create` row is also written for the new entry. Two rows for one user action is accurate, not a duplicate — the pedido line and the new weight entry are two different entities, each getting its own audit-queryable history, exactly like `Radiography`'s existing entry-plus-details model already expects.

## Open Questions

None blocking. Following up separately (per proposal Non-goals): the generic `Update*` paths and `ChangeTargetDocumentBehavior`, once their call patterns are mapped.
