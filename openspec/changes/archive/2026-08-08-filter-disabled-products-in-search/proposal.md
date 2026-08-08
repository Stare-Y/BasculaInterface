# Proposal: Filter Disabled Products in Search

## Why

`admProductos` (entity `Producto`) carries a soft-delete flag, `CSTATUSPRODUCTO` (`1` = enabled, `0` = disabled), but `ProductRepo.SearchByNameAsync` — the only method backing product discovery (`GET /api/Productos/ByName`) — never checks it. A disabled product can still be found and picked from every product-selection flow in the app (add product to a weight, and the "Cambiar producto" flow from issue #122). Issue #123.

## What Changes

- `ProductRepo.SearchByNameAsync` adds `p.CSTATUSPRODUCTO == 1` to its existing `Where` clause. That's the entire code change — no DTO, service, or controller signature changes.
- `GetByIdAsync` and `GetByMultipleIdsAsync` are explicitly left unfiltered (see Non-goals) — every UI flow that lets an operator *pick* a product sources the candidate ID from the now-filtered search, so this single change is sufficient to guarantee a disabled product can never be newly selected.

## Capabilities

### New Capabilities
- `product-search`: Product discovery (search by name/code, `GET /api/Productos/ByName`) excludes disabled (`CSTATUSPRODUCTO = 0`) products.

### Modified Capabilities
- None — no existing capability spec currently covers product search/discovery.

## Non-goals

- **Filtering `GetByIdAsync` / `GetByMultipleIdsAsync`.** These resolve products by an *already-known* ID rather than discover new ones. `WeightService.BuildContpaqiDocumentDto` calls `GetByIdAsync` to rebuild the ERP export document from a concluded weight's `WeightDetails`; filtering there would make document generation throw (or silently drop a line item) for a product disabled *after* it was weighed — a regression on the billing/ERP export path. Confirmed with the project owner during issue enrichment.
- Any write path or mutation of `CSTATUSPRODUCTO` — the ERP database is read-only for this app.
- Frontend (`BasculaUi`) changes — it has no product-fetching calls today.
- Any DTO (`ProductoDto`) shape change — this only narrows which rows are returned.

## Impact

**Affected terminals:** All (Main, Secondary, Pedidos-only) — anywhere product search/pick is used (add product to a weight; the "Cambiar producto" flow from `weight-detail-product-change`).
**API:** `BasculaTerminalApi` — `GET /api/Productos/ByName` only. `ByID`/`ByMultipleIds` unaffected.
**Client:** `BasculaInterface` (MAUI) — no code change needed; `ProductSelectorViewModel.SearchProducts` automatically receives the filtered result set from the same endpoint it already calls.
**Database:** Read-only `ContpaqiSQLContext` — additional `WHERE` predicate only, no migration.
