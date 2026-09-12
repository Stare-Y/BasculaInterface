# Design: Extend Delete Password Gate

## Context

Four soft-delete endpoints exist today with no password check:

| Endpoint | Service method | Repo call | UI caller |
|---|---|---|---|
| `DELETE api/Weight?id=` | `WeightService.DeleteAsync` | `WeightRepo.DeleteAsync` (sets `IsDeleted=true`, no cascade) | `DetailedWeightView.BtnDeleteEntry_Clicked` → `DetailedWeightViewModel.DeleteWeightEntry` |
| `DELETE api/Pedido?id=` | `PedidoService.DeleteAsync` | `PedidoRepo.DeleteAsync` (cascades `IsDeleted=true` to its `Lines`) | `PedidoFormView.BtnDelete_Clicked` → `PedidoFormViewModel.DeletePedidoAsync`; also `PedidoListViewModel.DeletePedidoAsync` (no View caller) |
| `DELETE api/Pedido/Line?id=` | `PedidoService.DeleteLineAsync` | `PedidoLineRepo.DeleteAsync` (sets `IsDeleted=true`) | none |

All three are plain soft deletes with no side effects to recompute (unlike `WeightDetail` deletion, which conditionally recomputes `BruteWeight` — see #125's `weight-detail-delete`). `Pedido`/`PedidoLine` have no concurrency token (`RowVersion`), so unlike every `Weight`-side guarded mutation, there is no `WeightConcurrencyException`/409 path to add here.

The three already-guarded weight-detail mutations (#121 partner, #122 product, #124 amount, #125 delete) all share one pattern: compare an incoming `PasswordHash` against `WeightSettings.ChangeProductPasswordHash` (`IOptions<WeightSettings>`), throw `UnauthorizedAccessException` on mismatch, and block once `WeightEntry.ConptaqiComercialFK > 0` while still allowing the action on a concluded entry. This change extends that same pattern to three more deletes.

**Key difference from #125's precedent:** #125 added a *new* endpoint (`PATCH Detail/{id}/Delete`) *alongside* the existing unguarded one, because the unguarded one has a real, permanent exemption (the empty-row "✕" button). None of the four targets here have an equivalent exemption — there is no "safe to delete without a password" case for a whole `WeightEntry`, a whole `Pedido`, or a `PedidoLine`. So the correct fix is to change the *existing* endpoint's contract in place (still `PATCH .../{id}/Delete`, still a `PasswordHash` body, same HTTP-verb reasoning as #125's Decision 1), not leave the old unguarded route reachable beside a new one.

## Goals / Non-Goals

**Goals:**
- Every reachable "delete a whole record" action for `WeightEntry`, `Pedido`, and `PedidoLine` requires the shared manager password.
- `PedidoLine` delete is gated even with no current UI caller, so wiring one up later doesn't reopen the gap.
- Reuse `WeightSettings.ChangeProductPasswordHash` — no new setting, no new hashing logic.
- `WeightEntry` delete additionally respects the ERP-document boundary already enforced on every sibling guarded mutation (new behavior — see Decision 2).

**Non-Goals:**
- React (`BasculaUi`) implementation.
- Cascading soft-delete of `WeightEntry.WeightDetails` when the parent entry is deleted — unchanged from today's behavior (`WeightRepo.DeleteAsync` doesn't touch child details now; this change doesn't add that either).
- Concurrency/409 handling for `Pedido`/`PedidoLine` — neither entity has a concurrency token today; not introduced here.
- Wiring `PedidoListViewModel.DeletePedidoAsync` into a View.
- Renaming `ChangeProductPasswordHash`.
- Auditing beyond what already exists.

## Decisions

### Decision 1: Change the existing endpoints in place; don't add parallel routes

**Chosen:** `WeightController`'s `[HttpDelete]` action and `PedidoController`'s `[HttpDelete]`/`[HttpDelete("Line")]` actions are replaced by `[HttpPatch("{id}/Delete")]` / `[HttpPatch("Line/{id}/Delete")]` actions taking a `PasswordHash` body, using `id` as a route parameter instead of a query string (matching `Weight/Detail/{id}/Delete`'s shape). The old bare `DELETE ?id=` routes are removed, not left dormant.

**Rejected:** Add new `PATCH .../{id}/Delete` routes alongside the existing `DELETE ?id=` ones, leaving the old ones in place (mirroring #125's structure literally).
**Why rejected:** #125 kept its old route specifically because it has a permanent legitimate caller (the empty-row button) that must keep working unguarded. None of these three do — an old unguarded `DELETE api/Weight?id=` left reachable would just be the same hole with a different name. Removing it is the actual fix; leaving it "for compatibility" would defeat the point of this change.

**Consequence:** This is a breaking API change for these three routes. Same deployment shape as every prior guarded-mutation change: API and MAUI client ship together.

### Decision 2: `WeightEntry` delete blocks once an ERP document exists; `Pedido`/`PedidoLine` delete does not

**Chosen:** `WeightService.DeleteSafelyAsync` adds the same `ConptaqiComercialFK > 0` check used by every `WeightDetail` guarded mutation (`WeightService.cs:378-379`, `427-428`, `485-486`, `572-573`) — reject with `InvalidOperationException` if the entry already has a Contpaqi document. It does **not** check `ConcludeDate`, consistent with those same siblings (a manager override is expected to work on a concluded entry). `PedidoService.DeleteSafelyAsync`/`DeleteLineSafelyAsync` add no equivalent check — `Pedido`/`PedidoLine` have no ERP-document field to check.

**Why:** `WeightEntry.DeleteAsync` today has *no* guard at all — not even the informal one the unguarded detail-delete has (empty rows only). Adding the password gate without also closing this gap would let an operator delete an entry that already has a real ERP document behind it, which every sibling guarded mutation on the same entity treats as a hard stop. This is new, stricter behavior for `WeightEntry` delete specifically, justified by matching existing precedent on the same aggregate — not requested verbatim by #133, but a direct consequence of "harness the existing gate" once the check is right there in every neighboring method.

**Rejected:** Leave `WeightEntry` delete's only rule as password-correct/incorrect, matching the literal unguarded behavior it's replacing.
**Why rejected:** Would create an inconsistency where changing an entry's product/partner/amount blocks on an existing ERP document, but deleting the entry outright — a strictly bigger action — does not. That's a gap in the gate's intent, not a preserved feature.

### Decision 3: `PedidoService` gains an `IOptions<WeightSettings>` dependency

**Chosen:** `PedidoService`'s constructor takes `IOptions<WeightSettings> weightSettingsOptions`, same as `WeightService`. `WeightSettings` is already registered via `services.Configure<WeightSettings>(...)` in `ConfigureServices.cs:16`, so no new DI registration is needed — just the new constructor parameter, and `PedidoService`'s registration (`ConfigureServices.cs:108`) resolves it automatically.

**Rejected:** Introduce a dedicated `IPasswordGate`/`IDeletePasswordValidator` service so `PedidoService` doesn't need to know about `WeightSettings` by name.
**Why rejected:** Would be a bigger refactor than this issue asks for, and would touch the three already-shipped `WeightService` guarded methods too (for consistency) — out of scope. `WeightSettings` living in `Core.Application.Settings` and being generically named around "weight" despite gating a `Pedido` action is an existing naming smell (same one flagged in #124/#125's open questions), not a new one this change should fix.

### Decision 4: No credit re-validation, no cascade changes

**Chosen:** None of the three new guarded methods call `ValidatePartnerCreditAsync` (deleting a record only removes exposure, same reasoning as #125's Decision 4) or change how deletion cascades — `PedidoRepo.DeleteAsync`'s existing cascade to `Lines` is reused as-is; `WeightRepo.DeleteAsync`'s existing no-cascade-to-details behavior is reused as-is.

**Why:** Both are pre-existing repo behaviors outside this issue's scope (adding a password gate), and changing them would be a separate, larger behavior change than "harness the existing gate a little more."

## API Surface

```
Changed (was DELETE, no body):
  PATCH /api/Weight/{id}/Delete
    body: { PasswordHash: string }
    200 OK  → { Data: "Deleted", Message: "Success" }
    400 Bad Request → wrong password / entry already has a Contpaqi document
    404 Not Found → entry does not exist (or already soft-deleted)

  PATCH /api/Pedido/{id}/Delete
    body: { PasswordHash: string }
    200 OK  → { Data: "Deleted", Message: "Success" }
    400 Bad Request → wrong password
    404 Not Found → pedido does not exist (or already soft-deleted)

  PATCH /api/Pedido/Line/{id}/Delete
    body: { PasswordHash: string }
    200 OK  → { Data: "Deleted", Message: "Success" }
    400 Bad Request → wrong password
    404 Not Found → line does not exist (or already soft-deleted)

Removed:
  DELETE /api/Weight?id={id}
  DELETE /api/Pedido?id={id}
  DELETE /api/Pedido/Line?id={id}

Unchanged:
  DELETE /api/Weight/Detail?id={id}   (empty-row "✕" button — untouched, see Non-Goals)
```

## Service Changes

**`IWeightService` / `WeightService`:**
- `Task<bool> DeleteAsync(int id)` → `Task<bool> DeleteSafelyAsync(int id, string passwordHash)`:
  1. Compare `passwordHash` against `_weightSettings.ChangeProductPasswordHash` (same empty-hash-never-matches, ordinal case-insensitive rule); throw `UnauthorizedAccessException` on mismatch.
  2. Load the entry (existing lookup the current `DeleteAsync` already does via `FindAsync` inside the repo — service layer needs the entry's `ConptaqiComercialFK` first, so fetch via the existing `GetByIdAsync`/repo accessor before deleting).
  3. If `entry.ConptaqiComercialFK > 0`, throw `InvalidOperationException` ("Este proceso ya cuenta con un documento en Contpaqi; no se puede eliminar.").
  4. `return await _weightRepo.DeleteAsync(id);` (existing, unchanged repo method).

**`IPedidoService` / `PedidoService`:**
- `Task<bool> DeleteAsync(int id)` → `Task<bool> DeleteSafelyAsync(int id, string passwordHash)`: same password check as above, then `return await _pedidoRepo.DeleteAsync(id);` (existing, unchanged).
- `Task<bool> DeleteLineAsync(int id)` → `Task<bool> DeleteLineSafelyAsync(int id, string passwordHash)`: same password check, then `return await _pedidoLineRepo.DeleteAsync(id);` (existing, unchanged).
- New constructor parameter `IOptions<WeightSettings> weightSettingsOptions` (Decision 3).

**`IWeightRepo`/`WeightRepo`, `IPedidoRepo`/`PedidoRepo`, `IPedidoLineRepo`/`PedidoLineRepo`:** No changes — existing `DeleteAsync` methods are reused as-is by the new service methods.

**Config:** None — reuses `WeightSettings.ChangeProductPasswordHash` as-is.

## Controller Changes

`WeightController`:
```csharp
public record DeleteWeightEntryRequest(string PasswordHash);

// Replaces the existing [HttpDelete] action
[HttpPatch("{id}/Delete")]
public async Task<IActionResult> DeleteSafely(int id, [FromBody] DeleteWeightEntryRequest request)
{
    try
    {
        bool deleted = await _weightService.DeleteSafelyAsync(id, request.PasswordHash);
        if (!deleted)
            return NotFound($"Weight entry with ID {id} not found.");

        return Ok(new GenericResponse<string> { Data = "Deleted", Message = "Success" });
    }
    catch (UnauthorizedAccessException)
    {
        return BadRequest(new GenericResponse<string> { Message = "Contraseña incorrecta." });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting weight entry with ID {Id}", id);
        return BadRequest(new GenericResponse<string> { Message = $"Error deleting weight entry: {ex.Message}" });
    }
}
```

`PedidoController`: same shape, twice — `[HttpPatch("{id}/Delete")]` replacing `[HttpDelete]`, and `[HttpPatch("Line/{id}/Delete")]` replacing `[HttpDelete("Line")]`, each taking a `DeletePedidoRequest(string PasswordHash)` / `DeletePedidoLineRequest(string PasswordHash)`, same `UnauthorizedAccessException` → 400 "Contraseña incorrecta." mapping, no `WeightConcurrencyException` catch (not applicable — see Context).

## MAUI Client Changes

- **`DetailedWeightViewModel.DeleteWeightEntry`**: takes a `string passwordPlaintext` parameter, hashes it via the existing `PasswordHasher.HashSha256Hex`, calls `PATCH api/Weight/{id}/Delete` via `_apiService.PatchAsync<T>` instead of `DeleteAsync`.
- **`DetailedWeightView.BtnDeleteEntry_Clicked`**: replace the `DisplayAlert` yes/no confirm with `DeleteDetailPopUp.ShowAsync(...)` (or a near-identical popup instance) to collect the password, following `StartDeleteDetailFlow`'s exact shape; on cancel/empty password, abort with no call.
- **`PedidoFormViewModel.DeletePedidoAsync`**: same treatment — takes a password, hashes it, calls the new `PATCH api/Pedido/{id}/Delete`.
- **`PedidoFormView.BtnDelete_Clicked`**: same popup swap as `BtnDeleteEntry_Clicked`.
- **`PedidoListViewModel.DeletePedidoAsync`**: same signature change (password parameter, hash, new endpoint) for consistency, even with no current View caller — so it isn't a dangling gap if/when a delete action is added to the pedido list.

## Risks / Trade-offs

**Risk: breaking change to three endpoint contracts** — accepted; every prior guarded-mutation change in this codebase has shipped API + MAUI client together, and this app has "no user management yet... solo developer project, currently in production" per project context, so a coordinated deploy is the established norm, not a new risk.

**Risk: `WeightEntry` delete's new ERP-document check is stricter than today's behavior** (Decision 2) — accepted; matches every sibling guarded mutation on the same entity, and an entry with a real ERP document being deletable at all today looks like an existing gap this change is well-positioned to close, not a regression.

**Risk: `PedidoService` taking on a `WeightSettings`-named dependency for an unrelated entity** — accepted, explicit trade-off in Decision 3; a proper rename/extraction is deferred, consistent with the same open question already carried since #124/#125.

## Migration Plan

1. Deploy API with the three changed routes (breaking for old `DELETE ?id=` callers — none exist outside this same MAUI client).
2. No `appsettings.json` change needed — reuses the existing configured hash.
3. Deploy updated MAUI client in the same release.
4. No database migration required.
5. Rollback: revert both API and client to the prior release together (not independently deployable, same as the routes being replaced in place).

## Open Questions

- **Audit trail**: same open question carried over from #122/#124/#125 — not requested by #133; recommend deciding once, for all guarded actions together, at implementation time.
- **Password setting name / `IPasswordGate` extraction**: same open question carried over from #124/#125, now slightly sharper since `PedidoService` also depends on `WeightSettings`. Deferred — not blocking this change.
