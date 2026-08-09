# Design: Change Weight Entry Partner

## Context

`WeightEntry.PartnerId` is set once via `BtnPickPartner` (`DetailedWeightView.xaml.cs:526-537`), which calls a plain `PUT /api/Weight` through `UpdateWeightEntry()` and then hides itself (`BtnPickPartner.IsVisible = false`). There's no path to correct the partner afterward — the only workaround today would be re-opening `BtnPickPartner`'s visibility manually (not exposed) or editing the database directly.

Issue #122 already introduced the first password-gated, credit-revalidated mutation pattern (`ChangeDetailProductAsync`/`PATCH /api/Weight/Detail/{id}/Product`) and the first custom row-level action ("⋮" menu, currently a native `DisplayActionSheet`). This change extends both: a second action on that same menu, and a restyle of the menu itself.

There is also pre-existing, unused scaffolding for a partner swap: `IWeightService.TrySwapPartner(int weightId, int currentPartnerId, int newPartnerId)`, `POST /api/Weight/Partner/Swap`, and `SwapPartnerRequest`. The service implementation is a no-op (`return;`), nothing in the client calls it, and it predates the password-gate/PATCH conventions established by issue #122.

## Goals / Non-Goals

**Goals:**
- Change `WeightEntry.PartnerId` on an existing entry, password-gated, with credit re-validation against the *new* partner.
- Block the change once the entry has a related Contpaqi document (`ConptaqiComercialFK > 0`), independent of `ConcludeDate`.
- Present this alongside "Cambiar producto" in a restyled, app-themed row menu (replacing `DisplayActionSheet`).
- Keep this a single, separate, RESTful endpoint (explicit issue requirement, same as #122).

**Non-Goals:**
- Real authentication/authorization.
- React (`BasculaUi`) implementation.
- Changing the initial `BtnPickPartner` assignment flow.
- Auditing beyond what's decided below.

## Decisions

### Decision 1: Reuse `GetByIdAsync` + `UpdateAsync(force: true)` — no repo changes needed

**Chosen:** `ChangePartnerAsync` loads the entry via the existing `_weightRepo.GetByIdAsync(weightId)` (already includes `WeightDetails`, `AsNoTracking`), mutates `PartnerId` on that detached instance, and persists via the existing `_weightRepo.UpdateAsync(entry, force: true)` — the same repo method `ChangeTargetDocumentBehavior` already uses to bypass the concluded-entry lock. `UpdateAsync`'s field whitelist (`WeightRepo.cs:267`) already includes `PartnerId`, so no repo changes are required.

**Contrast with issue #122:** that change discovered `WeightRepo.UpdateDetailAsync` silently dropped fields not in its whitelist and had to extend it. `WeightRepo.UpdateAsync` (the entry-level method) already whitelists `PartnerId` — verified by reading `WeightRepo.cs:244-269` — so this risk doesn't recur here.

**Rejected:** Add a new narrow repo method (e.g., `UpdateEntryPartnerAsync`).
**Why rejected:** Unnecessary — the existing `UpdateAsync(force: true)` already does exactly this with concurrency handling (`DbUpdateConcurrencyException` → `WeightConcurrencyException` → `409`) built in.

### Decision 2: Reuse `ChangeProductPasswordHash` rather than add a new setting

**Chosen:** `ChangePartnerAsync` compares the incoming `PasswordHash` against the same `WeightSettings.ChangeProductPasswordHash` value already configured for issue #122. One shared "manager override" secret gates both actions.

**Rejected:** Add a distinct `ChangePartnerPasswordHash` setting.
**Why rejected:** Doubles config surface for an already-explicit stopgap (a single shared password, not per-action). Reusing means production doesn't need a new value set before shipping. The property name (`ChangeProductPasswordHash`) becomes a mild misnomer now that it gates two actions — acceptable; renaming to something like `ManagerOverridePasswordHash` is a cheap, optional follow-up and not required for this change (flagged in Open Questions).

### Decision 3: Remove the dead `TrySwapPartner`/`Partner/Swap` scaffold instead of repurposing it

**Chosen:** Delete `IWeightService.TrySwapPartner`, its no-op implementation in `WeightService`, `WeightController`'s `POST /api/Weight/Partner/Swap` action, and the `SwapPartnerRequest` DTO. Replace with `ChangePartnerAsync`/`PATCH /api/Weight/{id}/Partner`, matching the naming (`ChangeXAsync`) and routing (`PATCH .../{id}/...`) conventions already used by `ChangeDetailProductAsync` and `ChangeTargetDocumentBehavior`.

**Rejected:** Add the password field and credit check to the existing `TrySwapPartner`/`SwapPartnerRequest`, keeping the `POST /api/Weight/Partner/Swap` route.
**Why rejected:** The old shape carries a redundant `currentPartnerId` (the server already knows the current partner from the loaded entity — no caller-supplied "current" value is needed to safely apply the change) and uses `POST` for what is semantically a partial update, inconsistent with every other single-field mutation in this controller. Since nothing calls it today, there's no compatibility cost to replacing it outright.

### Decision 4: Block on `ConptaqiComercialFK > 0`, not on `ConcludeDate`

**Chosen:** `ChangePartnerAsync` rejects (`400 Bad Request`) whenever `entry.ConptaqiComercialFK > 0` (the entry already has a related Contpaqi document, per the existing convention checked elsewhere — see `WeightService.cs:218` and `:177`). `ConcludeDate` is **not** checked; a concluded-but-not-yet-sent entry (`ConptaqiComercialFK` null/0) can still have its partner changed via `UpdateAsync(force: true)`.

**Confirmed with project owner during issue enrichment** — this deliberately diverges from issue #122's rule (which ignores `ConcludeDate` entirely and has no ERP-sent guard at all), because the partner is billing-relevant: once a Contpaqi document references the old partner, changing it afterward wouldn't retroactively correct that document (same class of risk noted in #122's design.md, Decision 3's risk note, for the product case).

### Decision 5: Credit-check the entry's *full* current cost against the new partner (not a delta)

**Chosen:** Compute `totalCost = Σ (detail.ProductPrice ?? 0) * quantity` across the entry's non-deleted `WeightDetails`, where `quantity` is `Weight` if captured (`> 0`) else `RequiredAmount ?? 0` — the same per-detail formula `ValidatePartnerCreditAsync` already uses internally. Call `ValidatePartnerCreditAsync(newPartnerId, totalCost)`. If `!IsValid`, reject with `400` and persist nothing.

**Why full cost, not a delta (contrast with issue #122's Decision 4):** `ValidatePartnerCreditAsync(partnerId, requestedAmount)` sums `partnerId`'s *other* pending entries' cost as `pendingEntriesCost`, then adds `requestedAmount`. Before this swap is persisted, the entry is still attributed to the *old* partner in the database, so `GetPendingWeightsByPartnerAsync(newPartnerId)` won't include it — none of this entry's cost is yet counted against the new partner. Passing only a delta (as #122 does for a same-entry price change) would undercount; the entry's entire cost is new exposure for the incoming partner.

**Rejected:** Skip the credit check when the new partner's `AvailableCredit` looks sufficient from a stale client-side value.
**Why rejected:** Same rationale as #122 Decision 4 — the password authorizes the identity change only, not a credit override; the server must re-validate authoritatively.

**Found during post-implementation review, fixed before archive:** if `newPartnerId == entry.PartnerId` (the operator re-picks the partner already assigned) and the entry isn't concluded, this entry is already inside `ValidatePartnerCreditAsync`'s own `pendingEntriesCost` sum for that partner — re-validating would double-count its cost and could spuriously reject a change that changes nothing. `ChangePartnerAsync` now skips the credit re-validation entirely when `newPartnerId == entry.PartnerId`, while still requiring the correct password and still performing the (harmless, idempotent) persistence — the auth gate doesn't get weaker just because the resulting value happens to match.

### Decision 6: Replace `DisplayActionSheet` with a custom, themed row-menu popup

**Chosen:** New `RowActionMenuPopUp` (`ContentView` + `TaskCompletionSource<string?>`, mirroring `PickQuantityPopUp`'s pattern), shown from `RowMenu_Clicked` instead of `DisplayActionSheet`. Presents "Cambiar producto" and "Cambiar socio" as two themed, tappable rows (styled consistently with `ChangeProductConfirmPopUp`'s dark overlay / `AppThemeBinding` colors / `Boton` style), plus a close/cancel affordance. Returns the selected action string (or `null` on cancel) to the caller, which then proceeds exactly as `RowMenu_Clicked` does today (opening `ProductSelectView` or, for the new action, `PartnerSelectView`).

**Rejected:** Keep `DisplayActionSheet`, just reorder/relabel its buttons.
**Why rejected:** `DisplayActionSheet` is OS-rendered — WinUI and Android draw it natively and neither picks up this app's theme colors or button styling. There's no way to "make it match" without leaving the native control. Confirmed with the project owner during enrichment.

## API Surface

```
New:
  PATCH /api/Weight/{id}/Partner
    body: { NewPartnerId: int, PasswordHash: string }
    200 OK  → { Data: "Updated", Message: "Success" }
    400 Bad Request → wrong password / entry already has a Contpaqi document / insufficient credit / partner not found
    404 Not Found → weight entry does not exist
    409 Conflict → concurrent modification (WeightConcurrencyException)

Removed:
  POST /api/Weight/Partner/Swap
```

## Service & Repo Changes

**`IWeightService` / `WeightService`:**
- Remove `Task TrySwapPartner(int weightId, int currentPartnerId, int newPartnerId)`.
- Add `Task ChangePartnerAsync(int weightId, int newPartnerId, string passwordHash)`:
  1. Compare `passwordHash` against `_weightSettings.ChangeProductPasswordHash` (ordinal, case-insensitive; reject if the configured hash is empty/unset) → `UnauthorizedAccessException` on mismatch.
  2. `WeightEntry entry = await _weightRepo.GetByIdAsync(weightId)` — 404 if missing (existing repo behavior).
  3. If `entry.ConptaqiComercialFK > 0`, throw `InvalidOperationException("Este proceso ya cuenta con un documento en Contpaqi; no se puede cambiar el socio.")`.
  4. Compute `totalCost` per Decision 5; call `ValidatePartnerCreditAsync(newPartnerId, totalCost)` — this internally fetches the new partner via `_clienteProveedorService.GetById`, so it also surfaces not-found for a bad partner id; no separate lookup is needed. Throw with the validation's `Message` if `!IsValid`.
  5. `entry.PartnerId = newPartnerId;` then `await _weightRepo.UpdateAsync(entry, force: true);`

**`IWeightRepo` / `WeightRepo`:** No changes — reuses existing `GetByIdAsync` / `UpdateAsync`.

**`WeightController`:** Remove `SwapPartner` action; add `ChangePartner` per the API Surface above, with the same exception-to-status-code mapping already used by `ChangeDetailProduct` (`WeightConcurrencyException` → `409`, `UnauthorizedAccessException` → `400` "Contraseña incorrecta.", other exceptions → `400` with `ex.Message`).

**DTOs:** Remove `SwapPartnerRequest`. Add `public record ChangePartnerRequest(int NewPartnerId, string PasswordHash);` in `WeightController.cs`, alongside `ChangeDetailProductRequest`.

## MAUI Client Changes

- **`Views/PopUps/RowActionMenuPopUp.xaml(.cs)`** (new): replaces the `DisplayActionSheet` call in `RowMenu_Clicked`. `ContentView` + `TaskCompletionSource<string?>`, styled like `ChangeProductConfirmPopUp` (dark overlay Grid, `AppThemeBinding` background, `Boton`-styled rows for "Cambiar producto" / "Cambiar socio", a "Cancelar" close button).
- **`DetailedWeightView.xaml.cs`**: `RowMenu_Clicked` awaits `RowActionMenuPopUp.ShowAsync(row.Description)` instead of `DisplayActionSheet`; branches on the returned action the same way it already branches on `DisplayActionSheet`'s result. The new "Cambiar socio" branch opens `PartnerSelectView` (same class already used by `BtnPickPartner_Clicked`), subscribes to `OnPartnerSelected`-equivalent handling, then shows a new `ChangePartnerConfirmPopUp`.
- **`Views/PopUps/ChangePartnerConfirmPopUp.xaml(.cs)`** (new): modeled directly on `ChangeProductConfirmPopUp` — partner name label, password `Entry`, Confirm/Cancel buttons, `TaskCompletionSource` pattern.
- **`DetailedWeightViewModel.cs`**: new `ChangePartnerAsync(int newPartnerId, string passwordPlaintext)` — hashes via `PasswordHasher.HashSha256Hex`, calls `PATCH api/Weight/{id}/Partner`, then refreshes `Partner`/`WeightEntry` and calls `FetchNewWeightDetails()` (or equivalent) on success. Throws on failure, matching `ChangeDetailProductAsync`'s convention — the view's catch block surfaces `ex.Message` via `DisplayAlert`.

No changes needed to `CanChangeProductMenu`/`RowMenuVisibilityConverter` — both menu items share the same visibility gate.

## Risks / Trade-offs

**Risk: shared password now gates two distinct, consequential actions** (product identity *and* billing partner) — accepted per Decision 2; rotating the hash still only requires an `appsettings.json` edit + restart, same as issue #122.

**Risk: `ConptaqiComercialFK` guard has a narrow race window** — if `SendToContpaqiComercial` runs concurrently with a partner change, both could theoretically pass their own checks before either persists. Not new: the same class of race exists for every other check-then-write path in this codebase (e.g., credit validation itself) and is accepted at this trust/scale level. `409 Conflict` from optimistic concurrency provides a partial backstop if the entry itself was rewritten.

**Risk: removing `TrySwapPartner`/`SwapPartnerRequest`** — confirmed unused via repo-wide search (no callers in `BasculaInterface` or elsewhere); safe to delete.

## Migration Plan

1. Deploy API: remove the dead `Partner/Swap` scaffold, add `PATCH /api/Weight/{id}/Partner` (additive/replacing an unused route — no breaking change to any real caller).
2. No `appsettings.json` change required if `ChangeProductPasswordHash` is already set from issue #122; otherwise set it now (shared by both actions).
3. Deploy updated MAUI client (row menu restyle + new "Cambiar socio" flow).
4. No database migration required.
5. Rollback: hide "Cambiar socio" in the row menu; the API endpoint can stay dormant with no side effects.

## Open Questions

- **Setting rename**: should `ChangeProductPasswordHash` be renamed to something partner-agnostic (e.g. `ManagerOverridePasswordHash`) now that it gates two actions? Cosmetic, deferrable — not required for this change.
- **Audit trail**: same open question issue #122 left unresolved — should a successful partner change be recorded anywhere (e.g., appended to `WeightEntry.Notes`)? Not requested; can be deferred.
