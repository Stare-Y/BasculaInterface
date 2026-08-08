# Design: Change Weight Detail Product

## Context

`WeightDetail.FK_WeightedProductId` and `WeightDetail.ProductPrice` are set once, at detail creation (`CreateDetailAsync`) or via the general `PUT /api/Weight` snapshot merge (`WeightEntryDto.ApplyTo`). There is no path to change just the product on an existing detail — correcting a miscategorized product today means deleting the detail (`DELETE /api/Weight/Detail`) and recreating it, which discards `Weight`, `RequiredAmount`, `Costales`, `Notes`, and `WeightedBy`.

`ProductPrice` is a point-in-time snapshot of `ProductoDto.Precio` (sourced from the external ContpaqiSQL `Producto.CPRECIO1`), copied onto the detail by the client at creation time. `ValidatePartnerCreditAsync` (already shipped) sums `ProductPrice * quantity` across a partner's pending (non-concluded) entries to gate credit.

No password/elevated-action concept exists anywhere in the system yet (issue #122 introduces the first one, explicitly as a stopgap).

## Goals / Non-Goals

**Goals:**
- Swap `FK_WeightedProductId` + `ProductPrice` on an existing detail without touching any other field.
- Gate the action behind a password check that's fast to configure and ship, per issue #122.
- Re-validate the partner's credit against the *additional* exposure the swap creates, using the existing `ValidatePartnerCreditAsync`, without double-counting the detail's own current cost.
- Allow the action even on a concluded `WeightEntry` (explicit product decision — the password is the override).
- Keep this a single, separate, RESTful endpoint (explicit issue requirement).

**Non-Goals:**
- Real authentication/authorization (per-user accounts, roles, tokens) — future work.
- React (`BasculaUi`) implementation.
- Auditing beyond what's decided below (see Open Questions).
- Changing quantity fields (`Weight`, `RequiredAmount`, `Costales`).

## Decisions

### Decision 1: Preserve captured fields; only swap product identity + price

**Chosen:** The new service method only mutates `FK_WeightedProductId` and `ProductPrice`. `Weight`, `Tare`, `SecondaryTare`, `RequiredAmount`, `Costales`, `Notes`, `WeightedBy`, `IsLoaded` are left exactly as they are.

**Rejected:** Delete + recreate the detail with the new product.
**Why rejected:** Loses all captured progress and doesn't match the issue's explicit "no matter what amount is captured so far" requirement.

### Decision 2: Shared password hash in appsettings, hashed client-side

**Chosen:** `WeightSettings` (or a new `SecuritySettings`) gets `ChangeProductPasswordHash` (SHA-256, hex, lowercase). The MAUI client computes `SHA256(input)` locally and sends the hex string in the request DTO. The server does a case-insensitive, ordinal string comparison against the configured hash (no need for constant-time comparison at this trust level — single shared secret, not a per-user credential — but implementers may add it cheaply if convenient).

**Rejected A:** Send the plaintext password and hash server-side.
**Why rejected:** Issue explicitly asks for the client to hash so the plaintext never needs to leave the device or be logged in API request logs.

**Rejected B:** Build a minimal user/roles table now.
**Why rejected:** Explicitly out of scope per the issue ("not permanent, but need to implement it quickly... I'm aware, we need to hit production quick").

**Note:** This is a shared secret, not per-user — it does not identify *who* approved the change. See Open Questions on audit trail.

### Decision 3: Allow the change even when the parent entry is concluded

**Chosen:** `ChangeDetailProductAsync` does **not** check `WeightEntry.ConcludeDate`, unlike `CreateDetailAsync`/`SetSecondaryTareAsync`/`RecordWeightAsync`.

**Rejected:** Reuse the same "concluded = read-only" guard as every other detail mutation.
**Why rejected:** Confirmed with the project owner — the password prompt is meant to let a manager correct a miscategorized product discovered *after* the process was concluded, which is exactly the scenario this feature exists for.

**Risk:** if `SendToContpaqiComercial` already ran for this entry (`ConptaqiComercialFK > 0`), the ERP document was built with the *old* product's code (`BuildContpaqiDocumentDto` reads `WeightDetail.FK_WeightedProductId` at send time). Changing the product afterward does **not** retroactively correct that already-sent document — this is a known limitation, not silently solved by this change. Flagged for the operator's awareness (out of scope to reconcile ERP documents here).

### Decision 4: Hard-block on insufficient credit; validate the incremental delta only

**Chosen:** Compute `oldCost = (detail.ProductPrice ?? 0) * quantity` and `newCost = newProduct.Precio * quantity`, where `quantity = detail.Weight > 0 ? detail.Weight : (detail.RequiredAmount ?? 0)`. If `newCost > oldCost`, call `ValidatePartnerCreditAsync(partnerId, newCost - oldCost)`; if the result is `IsValid == false`, reject with `400 Bad Request` and do not persist anything. If `newCost <= oldCost` (same or cheaper product), skip the credit check entirely — the swap can only reduce the partner's exposure.

**Rejected A:** Call `ValidatePartnerCreditAsync(partnerId, newCost)` directly (full new cost as `requestedAmount`).
**Why rejected:** `ValidatePartnerCreditAsync`'s `pendingEntriesCost` already sums this same detail's cost (at its *old* price, since the swap hasn't been persisted yet) across the partner's pending entries. Passing the full `newCost` as an additional `requestedAmount` would double-count this detail once at the old price (inside `pendingEntriesCost`) and again at the new price (as `requestedAmount`), overstating exposure and causing spurious rejections.

**Rejected B:** Let the password override credit too.
**Why rejected:** Confirmed with the project owner — credit-limit integrity is a separate concern from "who's allowed to relabel a product."

### Decision 5: New, separate endpoint (not folded into `PUT /api/Weight` or `CreateDetail`)

**Chosen:** `PATCH /api/Weight/Detail/{id}/Product`, matching the existing `PATCH .../ChangeTargetDocumentBehavior` and sibling `PUT .../SecondaryTare` / `.../Weight` route conventions already in `WeightController`.

**Why:** Explicit RESTful-separation requirement from the issue; also keeps the password-gate and concluded-entry-bypass logic isolated to one narrow code path instead of branching inside the general-purpose update/create paths.

## API Surface

```
New:
  PATCH /api/Weight/Detail/{id}/Product
    body: { NewProductId: int, PasswordHash: string }
    200 OK  → { } (or the updated WeightDetailDto — implementer's choice, follow WeightDetailDto for consistency)
    400 Bad Request → wrong password / invalid product / insufficient credit
    404 Not Found → detail or product does not exist
```

## Service & Repo Changes

**`IWeightService` / `WeightService`:**
- Add `Task ChangeDetailProductAsync(int detailId, int newProductId, string passwordHash)`:
  1. Compare `passwordHash` against configured hash; throw `UnauthorizedAccessException` (mapped to `400`/`401` by the controller) on mismatch.
  2. `WeightDetail detail = await _weightRepo.GetDetailByIdAsync(detailId)` — 404 if missing (existing repo behavior).
  3. `ProductoDto newProduct = await _productService.GetByIdAsync(newProductId)` — surfaces not-found for a bad product id.
  4. Compute `quantity`, `oldCost`, `newCost` as in Decision 4; if `newCost > oldCost`, call `ValidatePartnerCreditAsync(detail.WeightEntry.PartnerId ?? 0, newCost - oldCost)` and throw/return failure if `!IsValid`.
  5. Set `detail.FK_WeightedProductId = newProductId; detail.ProductPrice = newProduct.Precio;` then `await _weightRepo.UpdateDetailAsync(detail)`.
  - Deliberately **does not** check `WeightEntry.ConcludeDate` (Decision 3).
  - Deliberately **does not** call `RecomputeBruteWeightAsync` — `Weight` is unchanged by this operation.

**`IWeightRepo` / `WeightRepo`:** No new methods needed — reuses existing `GetDetailByIdAsync` / `UpdateDetailAsync` (which already carries the `WeightConcurrencyException` → `409` translation used by every other `Detail/{id}/...` endpoint).

**Config:**
- Add `ChangeProductPasswordHash` (string) to `WeightSettings` (or a new dedicated settings class if the team prefers not to grow `WeightSettings` further) and to `appsettings.json` / `appsettings.Development.json`.

## Controller Changes

`WeightController`:
```csharp
public record ChangeDetailProductRequest(int NewProductId, string PasswordHash);

[HttpPatch("Detail/{id}/Product")]
public async Task<IActionResult> ChangeDetailProduct(int id, [FromBody] ChangeDetailProductRequest request)
{
    try
    {
        await _weightService.ChangeDetailProductAsync(id, request.NewProductId, request.PasswordHash);
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

- **`DetailedWeightView.xaml`**: add a "⋮" `ImageButton`/`Border` to the detail row template, shown via a `PointerGestureRecognizer` `PointerEntered`/`PointerExited` pair (desktop hover) toggling its `IsVisible`, following the existing `Border.GestureRecognizers` pattern already used on rows.
- **`DetailedWeightView.xaml.cs`**: new handler opens `ProductSelectView`, subscribes to `OnProductSelected` (same event already used by `BtnNuevoProducto_Clicked`), then shows a new `ChangeProductConfirmPopUp` (product name + password `Entry` + confirm button) — modeled on `PickQuantityPopUp`'s `ContentView` + `TaskCompletionSource<T>` pattern.
- **`DetailedWeightViewModel.cs`**: new method `ChangeDetailProductAsync(int detailId, int newProductId, string passwordPlaintext)` — computes `SHA256` hex of `passwordPlaintext`, calls `PATCH api/Weight/Detail/{id}/Product`, refreshes the local `WeightEntry`/row on success, surfaces the server's error message via `DisplayAlert` on failure (wrong password, insufficient credit, concurrency conflict).

## Risks / Trade-offs

**Risk: stale ERP document after a post-conclusion swap** (see Decision 3) — accepted, out of scope to reconcile automatically.

**Risk: shared password leaks/gets guessed** — accepted per issue owner; explicitly a stopgap. Rotating the hash only requires an `appsettings.json` edit + restart.

**Risk: `ProductoDto.Precio` may differ from what the operator saw in the picker if the external ContpaqiSQL price changed between opening the picker and confirming** — same staleness window that already exists for `CreateDetailAsync` today; not a new risk introduced by this change.

## Migration Plan

1. Deploy API with the new endpoint (additive, no breaking changes to existing endpoints).
2. Set `ChangeProductPasswordHash` in production `appsettings.json`.
3. Deploy updated MAUI client.
4. No database migration required.
5. Rollback: remove/hide the new menu entry in the client; the API endpoint can stay dormant (unused) with no side effects.

## Open Questions

- **Audit trail**: should a successful override be recorded anywhere (e.g., appended to `WeightDetail.Notes`, or a new `LastUpdated`-style stamp)? Not requested by the issue; recommend appending a short note (e.g., `"Producto cambiado de {old} a {new} el {date}"`) to `detail.Notes` at implementation time, since it's low-cost and the endpoint already has all the needed values — but this is not a hard requirement of this change and can be deferred.
- **Hash comparison hardening**: plain ordinal string comparison is proposed (Decision 2); flagged here in case the implementer prefers `CryptographicOperations.FixedTimeEquals` for defense-in-depth despite the low stakes of a single shared secret.
