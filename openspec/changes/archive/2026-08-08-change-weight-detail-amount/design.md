# Design: Change Weight Detail Amount

## Context

`WeightDetail.Weight` and `WeightDetail.RequiredAmount` are set once — `Weight` via `CreateDetailAsync`/`RecordWeightAsync` (scale capture), `RequiredAmount` via `CreateDetailAsync` (manual entry, non-granel products). There is no path to correct either after the fact without deleting and recreating the detail (`DELETE /api/Weight/Detail`), which discards `FK_WeightedProductId`, `ProductPrice`, `Costales`, `Notes`, and `WeightedBy` too.

`RecordWeightAsync` (`PUT /api/Weight/Detail/{id}/Weight`) *can* overwrite an already-set `Weight`, but it (a) hard-blocks once `WeightEntry.ConcludeDate` is set, and (b) has no password gate — any terminal can call it freely. Neither behavior is what's wanted for a deliberate, after-the-fact correction.

`RequiredAmount` has no dedicated endpoint at all post-creation. `WeightRepo.UpdateAsync` (the general `PUT /api/Weight`) explicitly does not touch detail fields ("owned by dedicated endpoints" — `WeightRepo.cs:265`), and `WeightRepo.UpdateDetailAsync` — the method every dedicated detail endpoint funnels through — does not include `RequiredAmount` in its persisted-field whitelist at all. This is the same shape of bug #122 discovered and fixed for `FK_WeightedProductId`/`ProductPrice`.

Two precedents already exist for a password-gated override that bypasses the concluded lock but respects the Contpaqi document boundary:
- `ChangePartnerAsync` (#121): blocks on `ConptaqiComercialFK > 0`, ignores `ConcludeDate`.
- `ChangeDetailProductAsync` (#122): ignores `ConcludeDate`, but — a gap confirmed with the project owner during issue enrichment — has **no** `ConptaqiComercialFK` check at all.

## Goals / Non-Goals

**Goals:**
- Override `Weight` or `RequiredAmount` on an existing detail without touching any other field.
- Recompute `WeightEntry.BruteWeight` when `Weight` changes, exactly like `RecordWeightAsync` already does.
- Gate the action behind the same password as #122/#121 — no new config.
- Re-validate credit against only the incremental cost increase, same approach as `ChangeDetailProductAsync`.
- Allow the override on a concluded entry, but block it once a Contpaqi document exists (`ConptaqiComercialFK > 0`) — and retrofit that same block onto `ChangeDetailProductAsync`.
- Fix `WeightRepo.UpdateDetailAsync` to persist `RequiredAmount`.

**Non-Goals:**
- Real authentication/authorization (per-user accounts, roles, tokens).
- React (`BasculaUi`) implementation.
- Auditing beyond what's decided below (see Open Questions).
- Changing `Tare`, `SecondaryTare`, `Costales`, `Notes`, `WeightedBy`, `FK_WeightedProductId`, `ProductPrice`, `IsLoaded`.
- Renaming `ChangeProductPasswordHash`.

## Decisions

### Decision 1: One endpoint, exactly one field per request

**Chosen:** The request DTO carries both `NewWeight` (double?) and `NewRequiredAmount` (double?), but the service method requires **exactly one** of them to be non-null; supplying both or neither is a `400 Bad Request` (`ArgumentException`, not silently resolved by priority).

**Rejected A:** Two separate endpoints (`.../Weight` override and `.../RequiredAmount` override).
**Why rejected:** `PUT /api/Weight/Detail/{id}/Weight` already exists for a different purpose (first-time capture, unauthenticated, concluded-blocking) — reusing that route for the override would conflate two different authorization models under one path. A single new route with a discriminated body keeps the override surface (and its password/ERP-document rules) in one place, matching the issue's "a new method for this action too" ask without doubling the endpoint count.

**Rejected B:** Let both fields be supplied together and silently prioritize `Weight` (mirroring the existing "`Weight` wins for cost/quantity" convention).
**Why rejected:** That convention exists for *reading* an already-consistent detail (only one of the two is ever meaningfully non-zero for a given product type), not for a single write intending to change both at once — which the issue never asked for and which would make the request's intent ambiguous to audit later.

### Decision 2: Route — `PATCH /api/Weight/Detail/{id}/Amount`

**Chosen:** `PATCH /api/Weight/Detail/{id}/Amount`, matching the `PATCH .../Product` and `PATCH .../Partner` conventions from #122/#121.

**Why not `.../Weight`:** Already taken by `RecordWeightAsync` (`PUT`, different semantics/authorization).
**Why not `.../RequiredAmount`:** Doesn't cover the `Weight` case; "Amount" is the umbrella term already used in the domain (`RequiredAmount`) for "how much of this product."

### Decision 3: Blocked once a Contpaqi document exists; still bypasses the concluded lock

**Chosen:** `ChangeDetailAmountAsync` checks `entry.ConptaqiComercialFK > 0` and rejects (`InvalidOperationException`) if so — it does **not** check `ConcludeDate`. This exactly mirrors `ChangePartnerAsync`'s existing guard (`WeightService.cs:429-432`).

**Also chosen (retrofit):** `ChangeDetailProductAsync` gets the identical guard added. Confirmed explicitly with the project owner during issue enrichment: *"restrict only if document in erp created... that rule to change product too."* This is a correctness fix to already-shipped/archived #122 behavior, not new scope creep — #122's design doc already flagged the risk ("if `SendToContpaqiComercial` already ran... changing the product afterward does not retroactively correct that already-sent document") but left it as an accepted, unmitigated risk. This change closes that gap by blocking the action outright once the document exists, consistent with how #121 already handles the identical risk for partner changes.

**Rejected:** Leave `ChangeDetailProductAsync` as-is and only add the guard to the new endpoint.
**Why rejected:** Explicit instruction from the project owner — the inconsistency between `ChangePartnerAsync` (already guarded) and `ChangeDetailProductAsync` (unguarded) was called out as something to fix now, not defer.

### Decision 4: Credit re-validation on the incremental increase only

**Chosen:** Compute `oldQuantity = detail.Weight > 0 ? detail.Weight : (detail.RequiredAmount ?? 0)` (existing convention, unchanged) using the detail's state *before* the edit. Compute `newQuantity` from the *requested* new value: if `NewWeight` was supplied, `newQuantity = NewWeight` (Weight always wins once set, consistent with the existing convention); if `NewRequiredAmount` was supplied, `newQuantity = detail.Weight > 0 ? detail.Weight : NewRequiredAmount` (i.e. if the detail already has a captured `Weight`, editing `RequiredAmount` alone does not change the quantity used for cost — the same "Weight wins" rule that already governs every other cost calculation in this codebase). Then `oldCost = (detail.ProductPrice ?? 0) * oldQuantity`, `newCost = (detail.ProductPrice ?? 0) * newQuantity` (price is unchanged by this endpoint — only quantity moves). If `newCost > oldCost`, call `ValidatePartnerCreditAsync(partnerId, newCost - oldCost)` and reject on failure; otherwise skip the check entirely.

**Edge case, called out explicitly:** overriding `RequiredAmount` on a detail that already has `Weight > 0` is a legitimate request (e.g. correcting a stray manually-entered quantity on a bulk item that was, for whatever reason, also given a `RequiredAmount`) but has **no effect on cost/credit**, by the same "Weight wins" rule used everywhere else. This is not a bug — it's consistent with how `ValidatePartnerCreditAsync`, `ChangeDetailProductAsync`, and `SendToContpaqiComercial`'s `GetUnidadesFromProductAndDetail` already treat these two fields as not-both-meaningful for a single detail.

**Rejected:** Pass the full `newCost` as `requestedAmount`.
**Why rejected:** Same double-counting problem `ChangeDetailProductAsync`'s design already ruled out — `ValidatePartnerCreditAsync`'s `pendingEntriesCost` already counts this detail at its pre-edit cost.

### Decision 5: `BruteWeight` recompute only when `Weight` changes and the detail is loaded

**Chosen:** After persisting, call `_weightRepo.RecomputeBruteWeightAsync(detail.FK_WeightEntryId)` if and only if `NewWeight` was supplied and `detail.IsLoaded == true` — mirrors `RecordWeightAsync`'s existing `if (detail.IsLoaded) await RecomputeBruteWeightAsync(...)` guard exactly. A `RequiredAmount`-only edit never triggers this — `RecomputeBruteWeightAsync` sums `.Weight` only (`WeightRepo.cs:166-168`), so it would be a wasted round-trip.

### Decision 6: Reject non-positive override values

**Chosen:** `NewWeight <= 0` or `NewRequiredAmount <= 0` (whichever was supplied) throws `ArgumentOutOfRangeException`, mirroring `RecordWeightAsync`'s `weight <= 0` guard and `SetSecondaryTareAsync`'s `tare <= 0` guard. Clearing a quantity to zero is out of scope for this "correction" flow — deleting the detail remains the path for that.

### Decision 7: Fix `WeightRepo.UpdateDetailAsync` to persist `RequiredAmount`

**Chosen:** Add `existing.RequiredAmount = detail.RequiredAmount;` to `UpdateDetailAsync`'s field list. Safe for existing callers (`SetSecondaryTareAsync`, `RecordWeightAsync`, `ChangeDetailProductAsync`) since each of them loads the tracked entity via `GetDetailByIdAsync` first and mutates that same instance — `RequiredAmount` round-trips unchanged for all of them, exactly as `FK_WeightedProductId`/`ProductPrice` did when #122 made the equivalent fix.

## API Surface

```
New:
  PATCH /api/Weight/Detail/{id}/Amount
    body: { NewWeight: double?, NewRequiredAmount: double?, PasswordHash: string }
      - exactly one of NewWeight / NewRequiredAmount must be non-null and > 0
    200 OK  → { Data: "Updated", Message: "Success" }
    400 Bad Request → wrong password / both-or-neither field supplied / non-positive value /
                       ERP document already exists / insufficient credit
    404 Not Found → detail does not exist
    409 Conflict → concurrent modification (WeightConcurrencyException)

Modified:
  PATCH /api/Weight/Detail/{id}/Product  (from #122)
    — now additionally returns 400 Bad Request when WeightEntry.ConptaqiComercialFK > 0
```

## Service & Repo Changes

**`IWeightService` / `WeightService`:**
- Add `Task ChangeDetailAmountAsync(int detailId, double? newWeight, double? newRequiredAmount, string passwordHash)`:
  1. Compare `passwordHash` against `_weightSettings.ChangeProductPasswordHash` (same empty-hash-never-matches rule as `ChangeDetailProductAsync`); throw `UnauthorizedAccessException` on mismatch.
  2. Validate exactly one of `newWeight`/`newRequiredAmount` is supplied and `> 0`; throw `ArgumentException`/`ArgumentOutOfRangeException` otherwise.
  3. `WeightDetail detail = await _weightRepo.GetDetailByIdAsync(detailId)` — 404 if missing.
  4. `if (detail.WeightEntry.ConptaqiComercialFK > 0) throw new InvalidOperationException(...)`.
  5. Compute `oldQuantity`, `newQuantity`, `oldCost`, `newCost` per Decision 4; if `newCost > oldCost`, call `ValidatePartnerCreditAsync(detail.WeightEntry.PartnerId ?? 0, newCost - oldCost)` and throw on `!IsValid`.
  6. Set `detail.Weight = newWeight ?? detail.Weight;` or `detail.RequiredAmount = newRequiredAmount ?? detail.RequiredAmount;` (only the supplied one changes) → `await _weightRepo.UpdateDetailAsync(detail)`.
  7. If `newWeight.HasValue && detail.IsLoaded`, call `await _weightRepo.RecomputeBruteWeightAsync(detail.FK_WeightEntryId)`.
  - Deliberately does **not** check `WeightEntry.ConcludeDate` (Decision 3).
- **Modify** `ChangeDetailProductAsync`: add `if (detail.WeightEntry.ConptaqiComercialFK > 0) throw new InvalidOperationException("Este proceso ya cuenta con un documento en Contpaqi; no se puede cambiar el producto.");` right after loading the detail, before the credit check.

**`IWeightRepo` / `WeightRepo`:**
- `UpdateDetailAsync`: add `existing.RequiredAmount = detail.RequiredAmount;` to the persisted-field list (Decision 7). No new repo methods needed otherwise — reuses `GetDetailByIdAsync`/`UpdateDetailAsync`/`RecomputeBruteWeightAsync`, all of which already carry the `WeightConcurrencyException` → `409` translation.

**Config:** None — reuses `WeightSettings.ChangeProductPasswordHash` as-is.

## Controller Changes

`WeightController`:
```csharp
public record ChangeDetailAmountRequest(double? NewWeight, double? NewRequiredAmount, string PasswordHash);

[HttpPatch("Detail/{id}/Amount")]
public async Task<IActionResult> ChangeDetailAmount(int id, [FromBody] ChangeDetailAmountRequest request)
{
    try
    {
        await _weightService.ChangeDetailAmountAsync(id, request.NewWeight, request.NewRequiredAmount, request.PasswordHash);
        return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
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
        return BadRequest(new GenericResponse<string> { Message = ex.Message });
    }
}
```

## MAUI Client Changes

- **`Views/PopUps/RowActionMenuPopUp.xaml(.cs)`**: add a third button ("Cambiar peso" / "Cambiar cantidad" — label can switch on `row.IsGranel` at call time), `OnChangeAmountClicked` → `CloseWithResult("Cambiar peso")`, following the exact pattern of `OnChangeProductClicked`/`OnChangePartnerClicked`.
- **`Views/PopUps/ChangeAmountConfirmPopUp.xaml(.cs)`** (new): modeled directly on `ChangeProductConfirmPopUp` — shows the current value, a numeric `Entry` for the new value, a password `Entry`, Confirm/Cancel. Field label and keyboard reflect `row.IsGranel` (kg vs. count).
- **`DetailedWeightView.xaml.cs`**: extend the `RowMenu_Clicked` switch with a `"Cambiar peso"` case calling a new `StartChangeAmountFlow(row)` that shows `ChangeAmountConfirmPopUp` directly (no picker step needed, unlike product/partner) and then calls the ViewModel method.
- **`DetailedWeightViewModel.cs`**: new method `ChangeDetailAmountAsync(int detailId, bool isGranel, double newValue, string passwordPlaintext)` — hashes the password via the existing `PasswordHasher`, sends `NewWeight` or `NewRequiredAmount` depending on `isGranel`, calls `PATCH api/Weight/Detail/{id}/Amount`, then `FetchNewWeightDetails()` to refresh (this also picks up the recomputed `BruteWeight`/`TotalWeight`). Throws on failure — same convention as `ChangeDetailProductAsync`/`ChangePartnerAsync`; the View's code-behind catches and calls `DisplayAlert`.

## Risks / Trade-offs

**Risk: retrofitting `ChangeDetailProductAsync` changes already-shipped/archived behavior.** Accepted — explicitly requested by the project owner as a correctness fix, not a new feature; no consumer depends on being able to change a product after an ERP document exists (that was always a latent data-integrity gap, never a used capability).

**Risk: `RequiredAmount`-only edits on a detail that also has `Weight > 0` silently don't move cost/credit** (Decision 4 edge case) — accepted, consistent with existing codebase-wide convention; flagged in scenarios/tests so it's a documented behavior, not a surprise.

**Risk: shared password now gates three distinct actions** (product, partner, amount) under a name (`ChangeProductPasswordHash`) that no longer reflects its full scope — accepted as a naming-only issue, deferred (see proposal.md Non-goals).

## Migration Plan

1. Deploy API with the new endpoint and the `ChangeDetailProductAsync`/`UpdateDetailAsync` fixes (additive + bug fixes, no breaking changes to other endpoints).
2. No `appsettings.json` change needed — reuses the existing configured hash.
3. Deploy updated MAUI client.
4. No database migration required.
5. Rollback: remove/hide the new menu entry in the client; the new API endpoint can stay dormant with no side effects. The `ChangeDetailProductAsync`/`UpdateDetailAsync` fixes are safe to leave in place even if the rest of this change is rolled back.

## Open Questions

- **Audit trail**: same open question carried over from #122/#121 — should an override be recorded (e.g. appended to `Notes`)? Not requested by the issue; recommend deciding once, at implementation time, for all three override actions together rather than per-change.
- **Password setting name**: should `ChangeProductPasswordHash` be renamed to something scope-neutral (e.g. `ManagerOverridePasswordHash`) now that it gates three actions? Deferred per proposal.md Non-goals — flagged here in case the implementer wants to fold a rename into this change instead of a future one (would require a coordinated `appsettings.json` update in production).
