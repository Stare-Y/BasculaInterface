# Design: Filter Disabled Products in Search

## Context

`ProductRepo` (`src/backend/Infrastructure/Repos/ProductRepo.cs`) is the only place in the codebase that queries `ContpaqiSQLContext.Productos` directly. It exposes three methods:

- `SearchByNameAsync(string name, int page, int sizePage)` — backs `GET /api/Productos/ByName`, the sole discovery surface. Consumed by `ProductSelectorViewModel.SearchProducts` in the MAUI client, which is reused by both the "add product to weight" flow and the "Cambiar producto" flow (`weight-detail-product-change`, issue #122).
- `GetByIdAsync(int id)` — resolves a single product by an already-known ID. Called by `WeightService.BuildContpaqiDocumentDto` (once per `WeightDetail.FK_WeightedProductId`, to rebuild the ERP export document for a weight) and by `WeightService.ChangeDetailProductAsync` (to fetch the new product's current price after it was already picked via search).
- `GetByMultipleIdsAsync(int[] ids)` — bulk ID resolution, exposed via `GET /api/Productos/ByMultipleIds`. No internal caller found in this repo; `BasculaUi` (React) has no product-fetching calls today.

None of the three check `CSTATUSPRODUCTO` (soft-delete flag on `admProductos`: `1` = enabled, `0` = disabled). The ERP database is read-only for this app (confirmed, issue #123).

## Goals / Non-Goals

**Goals:**
- A disabled product can never be found via `SearchByNameAsync` / `GET /api/Productos/ByName`.
- Enabled products are unaffected — same rows, same ordering, same pagination behavior.

**Non-Goals:**
- Filtering `GetByIdAsync` / `GetByMultipleIdsAsync` — see Decision 1.
- Any change to `ProductoDto`, `IProductService`, or `ProductosController` signatures.
- Any write path to `CSTATUSPRODUCTO`.

## Decisions

### Decision 1: Filter discovery (`SearchByNameAsync`) only — leave `GetById*` unfiltered

**Chosen:** Add `p.CSTATUSPRODUCTO == 1` to `SearchByNameAsync`'s existing `.Where(...)` predicate. `GetByIdAsync` and `GetByMultipleIdsAsync` are left exactly as they are.

**Rejected:** Filter all three methods uniformly.
**Why rejected:** `WeightService.BuildContpaqiDocumentDto` calls `GetByIdAsync` per `WeightDetail.FK_WeightedProductId` to rebuild the ERP document for an already-concluded weight (`Movimientos` array, product code/almacen/units). If a product is disabled *after* being weighed — a realistic sequence, since weighing and ERP catalog maintenance happen independently — filtering `GetByIdAsync` would make that lookup throw `KeyNotFoundException` (per `ProductRepo.GetByIdAsync`'s existing not-found behavior), breaking document generation for a historical, already-legitimate transaction. `GetByMultipleIdsAsync` is filtered the same way for consistency with `GetByIdAsync` even though it currently has no in-repo caller — both are "resolve a known ID" operations, not discovery, and a future caller resolving historical IDs (e.g. rendering past weight details) should get the same safety guarantee.

Confirmed with the project owner during issue enrichment (issue #123 comment thread): this is a deliberate scope boundary, not an oversight.

**Why it's still sufficient:** Every UI flow that lets an operator *pick* a new product (`ProductSelectorViewModel.SearchProducts`, used by both "add product" and "Cambiar producto") sources its candidate IDs exclusively from `SearchByNameAsync`. A disabled product can therefore never be newly selected anywhere in the app, even though `GetById*` stays permissive for already-referenced IDs.

### Decision 2: Push the filter into the SQL predicate, not in-memory

**Chosen:** `p.CSTATUSPRODUCTO == 1` joins the existing `Where(p => p.CNOMBREPRODUCTO.Contains(name) || p.CCODIGOPRODUCTO.Contains(name))` predicate (EF Core translates the combined predicate to SQL).

**Rejected:** Fetch as today, then `.Where(p => p.CSTATUSPRODUCTO == 1)` after materialization.
**Why rejected:** Would fetch (and paginate) rows that get dropped afterward, silently shrinking a page's result count below `sizePage`, and does needless work against a live ERP-mirrored table.

## API Surface

No change. `GET /api/Productos/ByName` keeps its existing route, query parameters, and `ProductoDto[]` response shape — it simply returns fewer rows when disabled products would otherwise match.

## Service & Repo Changes

**`ProductRepo.SearchByNameAsync`:**
```csharp
return await _context.Productos
    .AsNoTracking()
    .Where(p => (p.CNOMBREPRODUCTO.Contains(name) || p.CCODIGOPRODUCTO.Contains(name))
             && p.CSTATUSPRODUCTO == 1)
    .Skip((page - 1) * sizePage)
    .Take(sizePage)
    .OrderBy(p => p.CNOMBREPRODUCTO)
    .ToListAsync();
```

No changes to `IProductRepo`, `IProductService`, `ProductService`, or `ProductosController`.

## Risks / Trade-offs

**Risk: none identified for the change itself.** It only narrows an existing read query along an indexed-by-convention status column on a table the app already reads in full.

**Accepted limitation (by design, see Decision 1):** a product disabled after being referenced by a historical `WeightDetail` remains resolvable via `GetByIdAsync`/`GetByMultipleIdsAsync` — this is intentional, not a residual bug.

## Migration Plan

1. Deploy API with the updated `Where` predicate — additive/narrowing only, no breaking change to the endpoint contract.
2. No client (`BasculaInterface`) deploy required — it consumes the same endpoint and simply receives fewer rows automatically.
3. No database migration.
4. Rollback: revert the one-line predicate change; no data or state to unwind.

## Open Questions

None — the only scope-defining decision (filter discovery only vs. everywhere) was resolved during issue enrichment.
