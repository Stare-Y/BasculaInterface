## 1. Backend — Repository

- [x] 1.1 In `ProductRepo.SearchByNameAsync` (`src/backend/Infrastructure/Repos/ProductRepo.cs`), extend the existing `Where` predicate to also require `p.CSTATUSPRODUCTO == 1`
- [x] 1.2 Confirm no other method in `ProductRepo`/`IProductRepo` needs the same predicate per this change's scope (`GetByIdAsync`/`GetByMultipleIdsAsync` intentionally excluded — see `design.md` Decision 1). Verified: `CSTATUSPRODUCTO` now appears only in `SearchByNameAsync`'s predicate; `GetByIdAsync`/`GetByMultipleIdsAsync` unchanged.

## 2. Verification

- [x] 2.0 `dotnet build BasculaTerminalApi/BasculaTerminalApi.csproj` succeeds — 0 warnings, 0 errors. Note: sandbox only has .NET SDKs 8.0.129/10.0.110 installed, not the pinned `8.0.415` in `global.json`; verified by temporarily setting `rollForward: latestMajor` in `global.json` for this build only, then reverting it (confirmed clean `git diff` afterward — no repo change).
- [x] 2.1 Manual/integration test: `GET /api/Productos/ByName?name=<term matching a disabled product>` does not return that product — confirmed by project owner
- [x] 2.2 Manual/integration test: `GET /api/Productos/ByName?name=<term matching an enabled product>` still returns it, with unchanged pagination/ordering behavior — confirmed by project owner
- [x] 2.3 Manual/integration test: `GET /api/Productos/ById?id=<a disabled product's id>` still returns it (confirms `GetByIdAsync` intentionally unfiltered) — confirmed by project owner
- [x] 2.4 Manual test: concluding/printing a weight whose `WeightDetail` references a product that has since been disabled still succeeds (confirms `BuildContpaqiDocumentDto` is unaffected) — confirmed by project owner
- [x] 2.5 Manual smoke test in the MAUI client (`BasculaInterface`): searching for a known disabled product's name/code in the product picker (add-product and "Cambiar producto" flows) returns no results for it — confirmed by project owner
