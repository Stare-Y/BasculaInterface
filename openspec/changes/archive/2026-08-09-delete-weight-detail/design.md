# Design: Delete Weight Detail

## Context

`WeightDetail` deletion already exists as a primitive: `DELETE api/Weight/Detail?id=` → `WeightService.DeleteDetailAsync(id)` → `WeightRepo.DeleteDetailAsync(id)`, which sets `IsDeleted=true` and `LastUpdated=DateTime.UtcNow` (`WeightRepo.cs:303-314`). It is a plain soft delete with **no password gate and no `BruteWeight` recompute**. That's safe *only* because the client currently exposes it via a single "✕" button that's visible only when the row is still fully empty (`Tare=0 && Weight=0 && SecondaryTare=null && !IsSecondaryTerminal` — `DetailedWeightView.xaml:634-644`): nothing captured means nothing to recompute and nothing risky to gate.

Issue #125 asks for delete to also be reachable from the row's "⋮" menu — the same menu that already hosts "Cambiar producto" (#122), "Cambiar socio" (#121), and "Cambiar peso"/"Cambiar cantidad" (#124) — for rows that **do** have captured data. That requires the same three protections those three already established:
- A password gate (shared `WeightSettings.ChangeProductPasswordHash`).
- A block once `WeightEntry.ConptaqiComercialFK > 0`, while still bypassing the `ConcludeDate` lock.
- Mutation safety for `BruteWeight`: #124 (`ChangeDetailAmountAsync`) established the precedent that any action touching a `WeightDetail.Weight` that's counted toward `BruteWeight` must recompute it, conditioned on `detail.IsLoaded`.

Deletion doesn't need the fourth protection those three share — credit re-validation — because removing a detail only *decreases* a partner's cost exposure; `ValidatePartnerCreditAsync` never needs to run.

## Goals / Non-Goals

**Goals:**
- Soft-delete an existing `WeightDetail` from the "⋮" menu, gated behind the same password as #122/#121/#124.
- Recompute `WeightEntry.BruteWeight` when the deleted detail was `IsLoaded=true`, exactly the shape of #124's Decision 5.
- Allow the deletion on a concluded entry, but block it once a Contpaqi document exists (`ConptaqiComercialFK > 0`).
- Leave the existing unguarded `DELETE api/Weight/Detail` endpoint and its empty-row "✕" button completely untouched.

**Non-Goals:**
- Real authentication/authorization (per-user accounts, roles, tokens).
- React (`BasculaUi`) implementation.
- A minimum-detail-count floor on `WeightEntry` (explicitly decided against — see Decision 3).
- Credit re-validation (see Decision 4).
- Auditing beyond what's already decided as an open question in #122/#121/#124.
- Restoring a soft-deleted detail.

## Decisions

### Decision 1: Route — `PATCH /api/Weight/Detail/{id}/Delete`, separate from the existing `DELETE` endpoint

**Chosen:** A new route, `PATCH /api/Weight/Detail/{id}/Delete`, carrying a `DeleteDetailRequest(string PasswordHash)` body — matching the `PATCH .../{id}/...` + request-record convention already used by `ChangeDetailProduct`/`ChangePartner`/`ChangeDetailAmount` (`WeightController.cs:333-400`). The existing `DELETE api/Weight/Detail?id=` (query-string only, no body) stays exactly as-is, still wired to the empty-row "✕" button.

**Rejected A:** Add an optional `PasswordHash` query parameter to the existing `DELETE api/Weight/Detail` endpoint and branch server-side on whether the detail has captured data.
**Why rejected:** Confirmed during issue enrichment — the project owner wants the new guarded path kept separate from the already-shipped empty-row path, not retrofitted into it. Branching server-side on "does this detail look empty" would also duplicate the client's own visibility check and create two sources of truth for what counts as "safe to delete without a password."

**Rejected B:** Use the `DELETE` HTTP verb for the new guarded endpoint too (`DELETE api/Weight/Detail/{id}` with a JSON body).
**Why rejected:** `DELETE` with a body is non-idiomatic in ASP.NET Core routing conventions and would collide in spirit with the existing `DELETE Detail?id=` route. `PATCH .../{id}/Delete` keeps this endpoint visually and structurally identical to its three siblings, all of which are semantically "guarded mutations," not pure REST-verb deletes.

### Decision 2: Blocked once a Contpaqi document exists; still bypasses the concluded lock

**Chosen:** `DeleteDetailSafelyAsync` checks `detail.WeightEntry?.ConptaqiComercialFK > 0` and rejects (`InvalidOperationException`) if so — it does **not** check `ConcludeDate`. This mirrors `ChangePartnerAsync`/`ChangeDetailProductAsync`/`ChangeDetailAmountAsync`'s identical guard.

**Confirmed during issue enrichment:** the issue's "no matter its status" phrasing was clarified to mean "not gated by `ConcludeDate`" — the ERP-document boundary is a separate, still-enforced rule, consistent with all three prior guarded mutations.

### Decision 3: No minimum-detail-count floor

**Chosen:** Deleting a detail is allowed even if it's the last remaining (non-deleted) detail on its `WeightEntry`, leaving the entry with zero details. No new guard is added.

**Confirmed during issue enrichment:** explicitly decided by the project owner. This also mirrors existing behavior elsewhere in the codebase — `ConcludeEntryAsync` (`WeightRepo.cs:219-243`) has no minimum-detail-count check either (`entry.WeightDetails.Any(d => !d.IsLoaded)` is vacuously `false` on an empty collection).

**Rejected:** Reject the delete if it would leave `entry.WeightDetails.Count(d => !d.IsDeleted) == 0`.
**Why rejected:** Explicit product decision — simpler behavior, consistent with the rest of the codebase's lack of a similar floor.

### Decision 4: No credit re-validation

**Chosen:** `DeleteDetailSafelyAsync` never calls `ValidatePartnerCreditAsync`. Removing a detail only *decreases* the sum of `ProductPrice * quantity` counted against the entry's partner — it can't push anyone over their credit limit.

**Rejected:** Re-validate anyway for symmetry with product/partner/amount changes.
**Why rejected:** Would be a no-op check on every call (a decrease or removal can never fail a credit ceiling check) — pure wasted latency with no behavioral value.

### Decision 5: `BruteWeight` recompute only when the deleted detail was loaded

**Chosen:** Capture `detail.IsLoaded` and `detail.FK_WeightEntryId` *before* deleting. After the soft delete succeeds, call `_weightRepo.RecomputeBruteWeightAsync(entryId)` if and only if the captured `IsLoaded == true` — mirrors `ChangeDetailAmountAsync`'s `if (newWeight.HasValue && detail.IsLoaded)` guard (`WeightService.cs:562-565`) and `RecordWeightAsync`'s pattern. A never-loaded detail was never counted in `BruteWeight` (`RecomputeBruteWeightAsync` sums `.Weight` only for `IsLoaded` details, `WeightRepo.cs:167-169`), so skipping the recompute for it is a correctness match, not just an optimization.

**Why this must run *after* the delete, not before:** `RecomputeBruteWeightAsync` reloads the entry with `Include(w => w.WeightDetails.Where(d => !d.IsDeleted))` (`WeightRepo.cs:162-163`) — the deleted detail must already have `IsDeleted=true` in the database by the time this query runs, or it would still be summed in via a stale in-memory reference. Since `DeleteDetailAsync` and `RecomputeBruteWeightAsync` are two separate `SaveChangesAsync` calls (no shared transaction currently exists across `WeightRepo` methods, consistent with how every other guarded mutation in this codebase composes two repo calls), the delete must complete first.

### Decision 6: Reuse the existing repo-layer `DeleteDetailAsync` and `RecomputeBruteWeightAsync` as-is — no repo changes

**Chosen:** `WeightService.DeleteDetailSafelyAsync` composes three existing/near-existing pieces: `_weightRepo.GetDetailByIdAsync(detailId)` (to check the password gate's target and the ERP guard, and to capture `IsLoaded`/`FK_WeightEntryId`), the existing `_weightRepo.DeleteDetailAsync(detailId)` (unchanged), and the existing `_weightRepo.RecomputeBruteWeightAsync(entryId)` (unchanged, conditional per Decision 5). No `IWeightRepo`/`WeightRepo` signature changes are needed.

**Rejected:** Add a new repo method, e.g. `DeleteDetailAndRecomputeAsync`, that does both in one call.
**Why rejected:** The existing repo methods are already the right shape and are reused as-is by every other guarded mutation (`ChangeDetailAmountAsync` composes `UpdateDetailAsync` + `RecomputeBruteWeightAsync` the same way) — introducing a combined repo method here would be an unnecessary, inconsistent one-off.

## API Surface

```
New:
  PATCH /api/Weight/Detail/{id}/Delete
    body: { PasswordHash: string }
    200 OK  → { Data: "Deleted", Message: "Success" }
    400 Bad Request → wrong password / ERP document already exists
    404 Not Found → detail does not exist (or already soft-deleted)
    409 Conflict → concurrent modification during the BruteWeight recompute (WeightConcurrencyException)

Unchanged:
  DELETE /api/Weight/Detail?id={id}   (existing, unguarded, empty-row-only client usage — no change)
```

## Service & Repo Changes

**`IWeightService` / `WeightService`:**
- Add `Task DeleteDetailSafelyAsync(int detailId, string passwordHash)`:
  1. Compare `passwordHash` against `_weightSettings.ChangeProductPasswordHash` (same empty-hash-never-matches rule as the other three); throw `UnauthorizedAccessException` on mismatch.
  2. `WeightDetail detail = await _weightRepo.GetDetailByIdAsync(detailId)` — propagates `KeyNotFoundException` if missing/already deleted.
  3. `if (detail.WeightEntry?.ConptaqiComercialFK > 0) throw new InvalidOperationException("Este proceso ya cuenta con un documento en Contpaqi; no se puede eliminar el detalle.");`
  4. Capture `bool wasLoaded = detail.IsLoaded;` and `int entryId = detail.FK_WeightEntryId;` before deleting.
  5. `await _weightRepo.DeleteDetailAsync(detailId);` (existing method, unchanged — soft delete).
  6. `if (wasLoaded) await _weightRepo.RecomputeBruteWeightAsync(entryId);` (Decision 5).
  - Deliberately does **not** check `WeightEntry.ConcludeDate` and does **not** call `ValidatePartnerCreditAsync` (Decisions 2, 4).
- Existing `DeleteDetailAsync(int id)` is untouched — still called by the unguarded `DELETE api/Weight/Detail` route.

**`IWeightRepo` / `WeightRepo`:** No changes (Decision 6).

**Config:** None — reuses `WeightSettings.ChangeProductPasswordHash` as-is.

## Controller Changes

`WeightController`:
```csharp
public record DeleteDetailRequest(string PasswordHash);

[HttpPatch("Detail/{id}/Delete")]
public async Task<IActionResult> DeleteDetailSafely(int id, [FromBody] DeleteDetailRequest request)
{
    try
    {
        await _weightService.DeleteDetailSafelyAsync(id, request.PasswordHash);
        return Ok(new GenericResponse<string> { Data = "Deleted", Message = "Success" });
    }
    catch (WeightConcurrencyException)
    {
        return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
    }
    catch (UnauthorizedAccessException)
    {
        return BadRequest(new GenericResponse<string> { Message = "Contraseña incorrecta." });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting detail entry with ID {Id}", id);
        return BadRequest(new GenericResponse<string> { Message = ex.Message });
    }
}
```
This is additive — the existing `[HttpDelete("Detail")]` action (`WeightController.cs:193-210`) is unchanged.

## MAUI Client Changes

- **`Views/PopUps/RowActionMenuPopUp.xaml(.cs)`**: add a fourth button ("Eliminar", styled distinctly/destructively from the other three), `OnDeleteClicked` → `CloseWithResult("Eliminar")`, following `OnChangeProductClicked`/`OnChangePartnerClicked`/`OnChangeAmountClicked`'s exact pattern.
- **`Views/PopUps/DeleteDetailConfirmPopUp.xaml(.cs)`** (new): shows the row's description and a password `Entry`, Confirm/Cancel — simpler than `ChangeAmountConfirmPopUp` since there's no "new value" to capture. `ContentView` + `TaskCompletionSource<string?>` pattern (returns just the password, or null on cancel), same shape as `ChangePartnerPopUp.ShowAsync`.
- **`DetailedWeightView.xaml.cs`**: extend the `RowMenu_Clicked` switch with an `"Eliminar"` case calling a new `StartDeleteDetailFlow(row)` that shows `DeleteDetailConfirmPopUp` directly, then calls the ViewModel method; on success, refresh the row collection the same way `StartChangeAmountFlow` does.
- **`DetailedWeightViewModel.cs`**: new method `DeleteWeightDetailSafelyAsync(int detailId, string passwordPlaintext)` — hashes the password via `PasswordHasher`, calls `PATCH api/Weight/Detail/{id}/Delete`, then `FetchNewWeightDetails()` to refresh (also picks up the recomputed `BruteWeight`/`TotalWeight` when applicable). Throws on failure — same convention as the other three ViewModel methods; the View's code-behind catches and calls `DisplayAlert`. Existing `DeleteWeightDetail`/`RemoveWeightEntryDetail` (unguarded, empty-row path) are untouched.

## Risks / Trade-offs

**Risk: two separate delete paths for `WeightDetail` now exist** (unguarded empty-row `DELETE`, guarded "⋮"-menu `PATCH .../Delete`) — accepted, explicit scope decision during enrichment; the empty-row path's own visibility rule (`Tare=0 && Weight=0 && ...`) already guarantees the two paths' inputs never meaningfully overlap in practice (a row visible to the guarded menu already has `CanChangeProductMenu` true, independent of the empty-row button's separate visibility trigger).

**Risk: no minimum-detail-count floor means an entry can be deleted down to zero details** (Decision 3) — accepted, explicit product decision; `ConcludeEntryAsync` already tolerates this same edge case today.

**Risk: two non-transactional `SaveChangesAsync` calls (delete, then recompute)** — if the process crashes between them, `IsDeleted=true` persists but `BruteWeight` is left stale (over-counting the deleted detail). Accepted: identical risk profile to every other two-step guarded mutation in this codebase (e.g. `ChangeDetailAmountAsync`'s update-then-recompute), not a new risk class introduced by this change.

## Migration Plan

1. Deploy API with the new endpoint (additive, no breaking changes to other endpoints).
2. No `appsettings.json` change needed — reuses the existing configured hash.
3. Deploy updated MAUI client.
4. No database migration required.
5. Rollback: remove/hide the new menu entry in the client; the new API endpoint can stay dormant with no side effects.

## Open Questions

- **Audit trail**: same open question carried over from #122/#121/#124 — should a delete be recorded (e.g. appended to the parent entry's `Notes`)? Not requested by the issue; recommend deciding once, at implementation time, for all four guarded actions together rather than per-change.
- **Password setting name**: should `ChangeProductPasswordHash` be renamed to something scope-neutral now that it gates four actions? Deferred, same as #124's open question — not blocking this change.
