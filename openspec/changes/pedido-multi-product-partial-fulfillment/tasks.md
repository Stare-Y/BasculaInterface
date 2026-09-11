## 0. Pre-work — confirm decisions with project owner

- [x] 0.1 Confirm the Almacén Code↔Name↔category mapping — **resolved: ship the migration with an empty `Almacenes` table, no seed rows.** The project owner inserts the real Code/Name rows manually to match the ERP config. Also resolved: **no unique constraint on `Almacen.Code`** (the ERP legitimately reuses codes; integrity risk accepted by the owner).
- [x] 0.2 Confirm the almacén-override persistence default — **resolved: one-time only**, per design.md's existing default. No design change needed.
- [x] 0.3 Confirm almacén selection UX — **resolved: mandatory operator pick every conversion**, pre-filled from the product default, always confirmed; empty list raises a client error and blocks the conversion. **Superseded by 0.5 on the source of the list.**
- [x] 0.5 Confirm almacén-target source — **resolved: no `Almacen` entity at all.** The almacén target is a hidden `ExternalTargetBehavior` row (the mechanism the old `ProviderPurchase` flow used); the operator picks among them. `ExternalTargetBehavior` gets an `AlmacenName` label column. The `Almacen` catalog (entity/repo/service/controller/DTO/table) and `WeightDetail.FK_AlmacenId` are deleted. **See section 10 for the rework.**
- [x] 0.4 Confirm dis-taring signal source — **resolved: superseded design.md Decision 6.** No `ProductWeighingConfig` table. `RequiresDisTaring` is a stored boolean on `PedidoLine` (operator-declared) and on `WeightDetail` (inherited at conversion, adjustable). The `CIDVALORCLASIFICACION6` magic-number mechanism is left untouched. **This reverses the originally-implemented approach — see section 9 for the rework.**

## 1. Domain entities

- [x] 1.1 Add `Core.Domain/Entities/ProviderOrders/Pedido.cs` (header) — `ProviderId`, `ExpectedArrival`, `Notes`, `Lines` collection
- [x] 1.2 Add `Core.Domain/Entities/ProviderOrders/PedidoLine.cs` — `PedidoId`, `ProductId`, `RequiredAmount`, `Price`, `Notes`, `ManuallyClosed`, `LastUpdated`
- [x] 1.3 ~~Add `Core.Domain/Entities/Warehousing/Almacen.cs`~~ — **superseded by 0.5; deleted in 10a.** Almacén target reuses hidden `ExternalTargetBehavior` (+ new `AlmacenName` column, 10b).
- [x] 1.4 ~~Add `Core.Domain/Entities/Products/ProductWeighingConfig.cs`~~ — **superseded by 0.4; deleted in 9a.1.** Dis-taring is now `RequiresDisTaring` on `PedidoLine` (1.2) + `WeightDetail` (1.5).
- [x] 1.5 Extend `Core.Domain/Entities/Weight/WeightDetail.cs`: add `int? FK_PedidoLineId` + nav, and `bool RequiresDisTaring` (9b.4). ~~`int? FK_AlmacenId`~~ was added then removed in 10a (superseded by 0.5).
- [x] 1.6 Delete `Core.Domain/Entities/ProviderOrders/ProviderPurchase.cs`

## 2. EF Core migration & DbContext

- [x] 2.1 Register new `DbSet<Pedido>`, `DbSet<PedidoLine>`, `DbSet<Almacen>`, `DbSet<ProductWeighingConfig>` on `WeightDBContext`; remove `DbSet<ProviderPurchase>`
- [x] 2.2 Generated `Infrastructure/Migrations/20260903021916_PedidosRemake.cs` (`dotnet ef migrations add PedidosRemake --context WeightDBContext`). Scaffolded with a throwaway connection string — `migrations add` only diffs the EF model, never connects — so the output is identical to running it against the real DB. Verified contents: `DropTable(ProviderPurchases)`; `CreateTable(Pedidos)`; `CreateTable(PedidoLines)` with `RequiresDisTaring boolean NOT NULL DEFAULT false` + FK→Pedidos `ON DELETE CASCADE`; `CreateTable(Almacenes)` — **no seed rows, PK only, no unique index on `Code`**; `WeightDetails` +`FK_PedidoLineId`/`FK_AlmacenId` (nullable, FK `ON DELETE SET NULL`) +`RequiresDisTaring boolean NOT NULL DEFAULT false`. No `ProductWeighingConfigs`. Snapshot stamped `ProductVersion 9.0.9`; API builds clean.
- [x] 2.3 Ship `Almacenes` table with no seed rows and no unique constraint on `Code` (per 0.1 resolution) — nothing to seed once the migration above is generated
- [x] 2.4 Migration applied by the owner against the real DB (2026-09-03) via `Program.cs`'s `db.Database.Migrate()`. `Almacenes` seeded manually by the owner.

## 3. Backend — Pedido repo & service (replaces ProviderPurchase)

- [x] 3.1 `IPedidoRepo`/`PedidoRepo`: `CreateAsync`, `GetByIdAsync` (Include Lines + their WeightDetails for computed amounts), `GetAllAsync`, `GetByProviderIdAsync`, `UpdateAsync`, `DeleteAsync` (soft, cascades to lines) — mirrors `ProviderPurchaseRepo`'s existing shape
- [x] 3.2 `IPedidoLineRepo`/`PedidoLineRepo`: `CreateAsync` (append to existing header), `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`, `CloseAsync` (sets `ManuallyClosed=true`)
- [x] 3.3 `PedidoDto`/`PedidoLineDto`: computed `ReceivedAmount`/`PendingAmount`/`Concluded` per design.md Decision 2 & 4 — **no stored counters**
- [x] 3.4 `IPedidoService`/`PedidoService`: CRUD passthroughs, following `ProviderPurchaseService`'s existing composition pattern
- [x] 3.5 `PedidoService.ConvertLineToWeightAsync(lineId, weightEntryId?, targetAmount?, almacenId?, externalTarget?)`: validates `targetAmount <= pending` and line not closed; creates/attaches `WeightDetail` with `FK_PedidoLineId` set ⚠️ HIGH-RISK: replaces the guarded, throw-on-second-attempt `CreateWeightEntryAsync` — must not reintroduce a 1:1 assumption anywhere in this path
- [x] 3.6 Delete `IProviderPurchaseRepo`/`ProviderPurchaseRepo`, `IProviderPurchaseService`/`ProviderPurchaseService`, `ProviderPurchaseDto`

## 4. Backend — Almacén & product-config services

- [x] 4.1 ~~`IAlmacenRepo`/`AlmacenRepo` + `IAlmacenService`/`AlmacenService`~~ — **superseded by 0.5; deleted in 10a.** Replaced by `ExternalTargetBehaviorService.GetAlmacenTargetsAsync()` (10b).
- [x] 4.2 ~~`IProductWeighingConfigRepo`/`ProductWeighingConfigRepo`~~ — **superseded by 0.4; deleted in 9a.1/9a.3**
- [x] 4.3 ~~Hydrate `RequiresDisTaring` via left join~~ — **superseded by 0.4; `ProductoDto.RequiresDisTaring` and the hydration removed in 9a.4/9a.5**

## 5. Backend — WeightService almacén resolution

- [x] 5.1 The `Precio` per-detail fix in `WeightService.BuildContpaqiDocumentDto` stands (`d.PedidoLine?.Price ?? d.ProductPrice ?? 0` — was a single global `purchasePrice` for every line item). The almacén-resolution edit was **reverted in 10a** back to the pre-change `product.IdAlmacen ?? entry.ExternalTargetBehavior.TargetAlmacen`.
- [x] 5.2 `WeightRepo.GetByIdAsync` includes `PedidoLine` on `WeightDetails`. The `.ThenInclude(wd => wd.Almacen)` was **removed in 10a**.

## 6. Backend — Controllers

- [x] 6.1 New `PedidoController`: header CRUD + line CRUD endpoints per design.md API Surface
- [x] 6.2 New `PedidoController` actions: `POST Line/{id}/ConvertToWeight`, `PATCH Line/{id}/Close` — exception mapping follows the existing pattern (`KeyNotFoundException`→404, `InvalidOperationException`→400); no `WeightConcurrencyException` mapping added since `PedidoLine` has no xmin concurrency token in this pass (see tasks.md §8 / design.md — not added, no concurrent-mutation path exists yet since pending is computed, not written)
- [x] 6.3 ~~New `AlmacenController`: `GET All`~~ — **superseded by 0.5; deleted in 10a.** Replaced by `GET /api/ExternalTargetBehavior/AlmacenTargets` (10b).
- [x] 6.4 Delete `ProviderPurchaseController`
- [x] 6.5 (unplanned, required for compilation) `WeightService` no longer depends on `IProviderPurchaseService`: removed the field/constructor param, replaced `ConcludeByWeightEntryAsync` call (no longer meaningful — see comment left in `ConcludeAsync`) and `GetByWeightEntryIdAsync`-based global `purchasePrice` (replaced with per-detail `PedidoLine.Price` resolution in `BuildContpaqiDocumentDto`, folded into task 5.1)

## 7. Frontend — MAUI client (thin orchestration only, per design.md's backend-only-logic constraint)

- [x] 7.1 Replaced `ProviderPurchaseListView(.xaml.cs)`/`ProviderPurchaseListViewModel` with `PedidoListView`/`PedidoListViewModel` — lists Pedido headers via `GET /api/Pedido/All*`, `PedidoViewRow` model shows provider/line-count/status
- [x] 7.2 Replaced `ProviderPurchaseFormView(.xaml.cs)`/`ProviderPurchaseFormViewModel` with `PedidoFormView`/`PedidoFormViewModel` — header form (provider/arrival/notes) + a lines `CollectionView` (`PedidoLineViewRow`) + an "Agregar Producto" panel that posts new `PedidoLine`s once the header exists
- [x] 7.3 New `ConvertLineToWeightPopUp` (ContentView + `TaskCompletionSource`, modeled on `ChangeAmountConfirmPopUp`): pending amount display, target-amount entry defaulted to full pending, `Almacen` picker backed by `GET /api/Almacen/All`; wired to `POST Line/{id}/ConvertToWeight` via `BtnConvertLine_Clicked`. Also added a "Cerrar" per-line action wired to `PATCH Line/{id}/Close`. Reworked in 9c: mandatory almacén pick (pre-selected from product default), empty-catalog error + block, "Requiere destare" checkbox pre-checked from the line.
- [~] 7.4 `RequiresDisTaring` surfacing in the MAUI weighing screen (`DetailedWeightView`/`DetailedWeightViewModel`) — still a follow-up. Under section 9 the source becomes `WeightDetailDto.RequiresDisTaring` (was `ProductoDto`/`PedidoLineDto`). Whether the screen auto-invokes dis-tare or just displays it is the open UX question (design.md Open Questions).
- [x] 7.5 Deleted `ProviderPurchaseViewRow`, `ProviderPurchaseListView(Model)`, `ProviderPurchaseFormView(Model)`; updated `MauiProgram.cs` DI registration and `PendingWeightsView.xaml.cs`'s navigation call to the new `PedidoListView`. **Not build-verified** — this sandbox has no `maui-android`/`maui-windows` workloads installed (confirmed via `dotnet workload list` and a failed `dotnet build`, `NETSDK1147`); same limitation noted in every prior MAUI change in this repo's history. Please build/smoke-test on your machine.

## 8. Verification

- [x] 8.1 Unit/manual test: converting a line twice (partial, then the remainder) succeeds and pending reaches exactly 0 without a third conversion being possible — covered by `PedidoPartialFulfillmentFlowTests.Converting_a_line_twice_reaches_exactly_zero_pending_and_blocks_a_third_conversion` (§12d, 2026-09-11)
- [x] 8.2 Unit/manual test: converting more than the current pending amount is rejected — covered by `PedidoServiceConvertLineToWeightTests.Throws_when_the_target_amount_exceeds_the_pending_amount` (unit) and `PedidoPartialFulfillmentFlowTests.Converting_more_than_the_pending_amount_is_rejected_with_400` (integration)
- [x] 8.3 Unit/manual test: `PATCH Line/{id}/Close` force-closes a line with pending > 0; `Concluded` reflects it immediately — pure DTO logic, covered by `PedidoDtoTests.Line_is_concluded_when_manually_closed_regardless_of_pending` (§12a, 2026-09-11)
- [x] 8.4 Unit/manual test: a `Pedido` header's computed `Concluded` flips to true only once every line is concluded (natural or manual) — covered by `PedidoDtoTests.Pedido_is_concluded_once_every_line_is_concluded_naturally_or_manually` + `Pedido_is_not_concluded_while_any_line_is_open` + `Pedido_with_no_lines_is_never_concluded` (§12a, 2026-09-11)
- [ ] 8.5 Manual test: ERP document submission resolves almacén as `product.IdAlmacen ?? weightEntry.ExternalTargetBehavior.TargetAlmacen` — granel product keeps its hardcoded code; non-granel uses the picked hidden behavior's `TargetAlmacen` ⚠️ HIGH-RISK path (Contpaqi document submission) — needs real ERP; owner-gated, not automatable here
- [x] 8.6 ~~Manual test: RequiresDisTaring on WeightDetail~~ — moot, `WeightDetail.RequiresDisTaring` was removed in §11a (reverted)
- [x] 8.7 Regression test: non-pedido `WeightEntry`/`WeightDetail` flows (manual weigh-ins with no `FK_PedidoLineId`) are unaffected — the non-discharge `BruteWeight` path (the flow every non-pedido weigh-in uses) is covered by `WeightRepoDischargeTests.Normal_entry_adds_loaded_weight_to_tare` + `UpdateAsync_on_a_normal_entry_is_unaffected_by_the_discharge_branch` (§12b, 2026-09-11); no `FK_PedidoLineId`/`RequiresDisTaring` field remains to regress on non-pedido `WeightDetail`s (grep-confirmed clean in §11c.5)
- [ ] 8.9 Manual test: the convert dialog forces an almacén-target choice (pre-filled by matching product `IdAlmacen` to a hidden behavior's `TargetAlmacen`) and shows an error instead of submitting when `GET /api/ExternalTargetBehavior/AlmacenTargets` is empty; server rejects a new-entry conversion with no/ non-hidden `ExternalTarget` — server-side half covered by `PedidoServiceConvertLineToWeightTests.Throws_when_creating_a_new_entry_without_a_valid_almacen_target` / `Throws_when_the_picked_target_is_not_a_hidden_almacen_behavior`; the MAUI dialog UX (pre-fill, empty-catalog error) remains owner smoke-test only
- [x] 8.10 Grep test: `grep -rn "ProductWeighingConfig"` over `src/` is clean (no migration files exist yet either)
- [ ] 8.8 Migration test: fresh database applies the new migration cleanly (**pending 2.2**); `grep -ri providerpurchase` confirmed clean except: historical migration files (expected, must not be edited), doc-comments in new files that reference the old model for context, and the not-yet-reworked MAUI files tracked under section 7

## 9. Rework from the Decision 5 / 6 revision (explore pass, post-implementation)

The backend was first built with a `ProductWeighingConfig` table and a silently-defaulted almacén. Owner decisions 0.3 and 0.4 changed both. Work needed:

### 9a. Delete `ProductWeighingConfig` entirely
- [x] 9a.1 Deleted `Core.Domain/Entities/Products/ProductWeighingConfig.cs` (+ its now-empty `Products/` dir), `Core.Domain/Interfaces/IProductWeighingConfigRepo.cs`, `Infrastructure/Repos/ProductWeighingConfigRepo.cs`
- [x] 9a.2 Removed `DbSet<ProductWeighingConfig>` and its `using Core.Domain.Entities.Products;` from `WeightDBContext` (there was no `OnModelCreating` config for it)
- [x] 9a.3 Removed the DI registration in `ConfigureServices.cs`
- [x] 9a.4 `ProductService`: dropped `IProductWeighingConfigRepo` and `HydrateDisTaringAsync`; the three lookups now return plain `ProductoDto`s
- [x] 9a.5 `ProductoDto`: removed `RequiresDisTaring` and its doc-comment

### 9b. Move dis-taring onto the line + detail
- [x] 9b.1 `PedidoLine`: added `public bool RequiresDisTaring { get; set; } = false;`
- [x] 9b.2 `PedidoLineDto`: `RequiresDisTaring` mapped from the entity in the ctor and back in `ToEntity()`; also persisted in `PedidoLineRepo.UpdateAsync`; comment updated
- [x] 9b.3 `PedidoService`: dropped `IProductWeighingConfigRepo`, `ToHydratedDtoAsync` and `HydrateLineAsync` — all reads now `new PedidoDto(entity)` / `new PedidoLineDto(entity)`
- [x] 9b.4 `WeightDetail`: added `public bool RequiresDisTaring { get; set; } = false;`
- [x] 9b.5 `WeightDetailDto`: carried `RequiresDisTaring` through ctor + `ToEntity()`
- [x] 9b.6 `PedidoService.ConvertLineToWeightAsync` (+ `IPedidoService`): optional `bool? requiresDisTaring`; `newDetail.RequiresDisTaring = requiresDisTaring ?? line.RequiresDisTaring`
- [x] 9b.7 `ConvertLineToWeightRequest` record: added optional `RequiresDisTaring`, passed through the controller action. `POST`/`PUT /api/Pedido/Line` round-trip the line flag via the DTO

### 9c. MAUI — force almacén pick + dis-tare checkbox (extends 7.3)
- [x] 9c.1 Add-line panel gets a "Requiere destare" `CheckBox` (`ChkNewLineDisTaring`); `PedidoLineViewRow.RequiresDisTaring` + a "Requiere destare" badge on each line row; `AddLineAsync` takes `bool requiresDisTaring` → `PedidoLineDto.RequiresDisTaring`
- [x] 9c.2 `ConvertLineToWeightPopUp.ShowAsync` takes `defaultAlmacenCode` (from `PedidoLineViewRow.DefaultAlmacenCode`, sourced from `ProductoDto.IdAlmacen` in `ReloadAsync`) and pre-selects the matching row; `OnPopupAcceptClicked` blocks on no selection; result tuple's `AlmacenId` is now non-nullable
- [x] 9c.3 `BtnConvertLine_Clicked` shows an error and returns when `ViewModel.Almacenes` is empty; `ShowAsync` also returns null on an empty list as a guard
- [x] 9c.4 Popup has a "Requiere destare" `CheckBox` pre-checked from `row.Line.RequiresDisTaring`; its value flows through `ConvertLineToWeightAsync` into the request body
- [ ] 9c.5 Not build-verifiable in this sandbox (no MAUI workloads, `NETSDK1147`) — owner builds/smoke-tests. Backend (`BasculaTerminalApi`) builds clean (0 warn / 0 err).

### 9d. Artifacts / bookkeeping
- [x] 9d.1 `grep -rn "ProductWeighingConfig"` over `src/` is clean; API project builds clean
- [ ] 9d.2 `openspec/specs/` sync (or archive) reflects the final `almacen-target-selection` and `product-dis-tare-signal` specs — do at archive time

## 10. Rework from the Decision 5 revision (owner: use hidden `ExternalTargetBehavior`, drop the `Almacen` catalog)

Owner decision 0.5 (2026-09-03): the almacén target is a hidden `ExternalTargetBehavior` — the mechanism the old `ProviderPurchase` flow defaulted to — picked by the operator. No `Almacen` entity.

### 10a. Delete the `Almacen` catalog + revert its integrations
- [x] 10a.1 Deleted `Core.Domain/Entities/Warehousing/Almacen.cs` (+ dir), `IAlmacenRepo`/`AlmacenRepo`, `IAlmacenService`/`AlmacenService`, `AlmacenDto`, `AlmacenController`
- [x] 10a.2 `WeightDBContext`: removed `DbSet<Almacen>`, the `wd.HasOne(d => d.Almacen)` config, and the `Core.Domain.Entities.Warehousing` using
- [x] 10a.3 `ConfigureServices.cs`: removed `IAlmacenRepo`/`IAlmacenService` registrations
- [x] 10a.4 `WeightDetail`: removed `FK_AlmacenId` + `Almacen` nav + the using; `WeightDetailDto`: removed `FK_AlmacenId` (ctor + `ToEntity()`)
- [x] 10a.5 `WeightService.BuildContpaqiDocumentDto`: almacén resolution back to `product.IdAlmacen ?? weightEntry.ExternalTargetBehavior.TargetAlmacen` (pre-change form); comment updated
- [x] 10a.6 `WeightRepo.GetByIdAsync`: removed `.ThenInclude(wd => wd.Almacen)`
- [x] 10a.7 `PedidoService`: dropped `IAlmacenRepo` (and the now-unused `ILogger`); `ConvertLineToWeightAsync` loses the `int? almacenId` param; a **new** weight entry now requires `externalTarget` to parse to a **hidden** `ExternalTargetBehavior` id (else `InvalidOperationException`); appending to an existing entry ignores it. `IPedidoService` + `ConvertLineToWeightRequest` updated (drop `AlmacenId`).

### 10b. `ExternalTargetBehavior` as the almacén-target source
- [x] 10b.1 `ExternalTargetBehavior`: add `string? AlmacenName`
- [x] 10b.2 `ExternalTargetBehaviorDto`: add `TargetAlmacen`, `AlmacenName`, `Hidden`, and `AlmacenDisplayText => "{TargetAlmacen} - {AlmacenName}"`; map in ctor. Existing `DisplayText` / non-hidden `GetAllAsync` path untouched.
- [x] 10b.3 `IExternalTargetBehaviorService` + service: `GetAlmacenTargetsAsync()` → the `Hidden == true` rows, ordered by `TargetAlmacen`
- [x] 10b.4 `ExternalTargetBehaviorController`: `GET /api/ExternalTargetBehavior/AlmacenTargets`

### 10c. MAUI
- [x] 10c.1 `PedidoFormViewModel`: `Almacenes` (`AlmacenDto`) → `AlmacenTargets` (`ExternalTargetBehaviorDto`); `LoadAlmacenesAsync` → `LoadAlmacenTargetsAsync` hitting `api/ExternalTargetBehavior/AlmacenTargets`; `ConvertLineToWeightAsync` sends `ExternalTarget = almacenTargetId.ToString()` (no more `AlmacenId`, no more `PurchaseExternalTarget` preference in this path)
- [x] 10c.2 `ConvertLineToWeightPopUp`: picker of `ExternalTargetBehaviorDto` displaying `AlmacenDisplayText`; pre-selects the row whose `TargetAlmacen == defaultAlmacenCode`; result tuple carries `AlmacenTargetId` (the ETB id)
- [x] 10c.3 `PedidoFormView.xaml.cs`: `LoadAlmacenTargetsAsync`, `ViewModel.AlmacenTargets` empty-check + error, updated `ShowAsync` / `ConvertLineToWeightAsync` args
- [x] 10c.4 `PedidoLineViewRow.DefaultAlmacenCode` unchanged (still the product's `IdAlmacen`); now matched against `TargetAlmacen` in the popup
- [ ] 10c.5 Not build-verifiable here (no MAUI workloads). Backend + test projects build clean (0/0).

### 10d. Migration & bookkeeping
- [x] 10d.1 Generated `Infrastructure/Migrations/..._DropAlmacenCatalogUseExternalTargetBehavior.cs` (scaffold-only connection string): `DropTable(Almacenes)`; drop `WeightDetails.FK_AlmacenId` (+ FK + index); `AddColumn(ExternalTargetBehaviors.AlmacenName text NULL)`. `RequiresDisTaring` columns kept. Snapshot `ProductVersion 9.0.9`; API builds clean.
- [ ] 10d.2 **Owner action:** apply the migration (run the API), then seed the extra hidden `ExternalTargetBehavior` rows (COMPRA PRODUCCION, same `TargetConcept`, differing `TargetAlmacen` + `AlmacenName`), and set `AlmacenName` on the existing hidden row.
- [x] 10d.3 `grep -rn "AlmacenDto\|IAlmacen\|FK_AlmacenId"` over `src/` (excl. migrations) is clean

## 11. Dis-tare rollback + discharge-direction fix (owner feedback, post-smoke-test)

Owner feedback while validating section 10 (2026-09-04): `WeightDetail.RequiresDisTaring` never touched the weighing screen at all — undo it. Separately, a real bug: pedido deliveries run the scale backwards (truck arrives full, discharges, leaves lighter), but `BruteWeight` always adds, and the printed ticket read as the vehicle "gaining" weight. See design.md Decision 6 (revert note) and Decision 7.

### 11a. Undo the WeightDetail dis-tare gate
- [x] 11a.1 Removed `WeightDetail.RequiresDisTaring` (entity + doc comment); `WeightDetailDto` (property, ctor mapping, `ToEntity()`)
- [x] 11a.2 `PedidoService.ConvertLineToWeightAsync` (+ `IPedidoService`): dropped the `requiresDisTaring` parameter and the inheritance line; `ConvertLineToWeightRequest` (`PedidoController`) dropped the field and the passthrough
- [x] 11a.3 MAUI: `ConvertLineToWeightPopUp` (`.xaml`/`.xaml.cs`) — removed the "Requiere destare" checkbox/label, `ShowAsync`/result-tuple no longer carry it; `PedidoFormViewModel.ConvertLineToWeightAsync` and `PedidoFormView.xaml.cs`'s `BtnConvertLine_Clicked` updated to match
- [x] 11a.4 Kept unchanged (owner's explicit choice): `PedidoLine.RequiresDisTaring` + `PedidoLineDto`/`PedidoLineRepo` mapping, the "Requiere destare" badge on each line row, `ChkNewLineDisTaring` in the add-line panel, `PedidoFormViewModel.AddLineAsync(..., requiresDisTaring)` — informational only
- [x] 11a.5 `product-dis-tare-signal` spec rewritten to reflect the revert

### 11b. `WeightEntry.IsDischarge` — discharge-direction fix
- [x] 11b.1 `WeightEntry`: added `bool IsDischarge` (default `false`) + doc comment; `WeightEntryDto`: added the field, mapped in ctor/`ToEntity()`/`ApplyTo()`
- [x] 11b.2 `WeightRepo`: new private `ComputeBruteWeight(entry, loadedSum)` helper — `TareWeight - loadedSum` when `IsDischarge` (throws `InvalidOperationException` if `loadedSum > TareWeight`), else `TareWeight + loadedSum`. Used by `RecomputeBruteWeightAsync`, `MarkDetailLoadedAsync`, and `UpdateAsync` (which also now copies `IsDischarge` from the incoming DTO)
- [x] 11b.3 `PedidoService.ConvertLineToWeightAsync`: the new-`WeightEntry` branch sets `IsDischarge = true` — a pedido is always a provider delivery. The "append to existing entry" branch is untouched (inherits whatever the entry was created with)
- [x] 11b.4 MAUI `DetailedWeightViewModel.TotalWeight`: same direction branch as the server, so the live-weighing total falls (not climbs) during a discharge
- [x] 11b.5 `PrintService.BuildWeightHeader`/`BuildWeightDetailsTable`: the "initial weight" and "final weight" ticket labels read `entry.IsDischarge` directly (`"TARA INICIAL:"`/`"BRUTO:"` → `"PESO INICIAL (CARGADO):"`/`"PESO FINAL (VACÍO):"`) instead of leaving direction implicit; the `"Proveedor:"/"Socio:"` partner-identity label and per-product `"Tara:"/"Neto:"` rows are untouched
- [x] 11b.6 Confirmed unchanged/correct already: the client-side `Math.Abs` diff capture in `BasculaViewModel` (`CaptureNewWeightEntry`/`UpdateWeight`/`SetTara`) — no edit needed
- [x] 11b.7 New `weight-entry-discharge-direction` spec added

### 11c. Migration & verification
- [x] 11c.1 Generated `Infrastructure/Migrations/20260904212654_DischargeDirectionAndDisTaringRollback.cs` (scaffold-only connection string): `DropColumn(WeightDetails.RequiresDisTaring)`; `AddColumn(WeightEntries.IsDischarge boolean NOT NULL DEFAULT false)`. Snapshot updated; both `BasculaTerminalApi` and `BasculaTerminalTest` build clean (0/0)
- [ ] 11c.2 **Owner action:** apply the migration
- [x] 11c.3 Manual test: a discharge entry's `BruteWeight` falls as products are captured, never exceeds `TareWeight`, and the printed ticket shows `"PESO INICIAL (CARGADO):"` (high) / `"PESO FINAL (VACÍO):"` (low) — never an apparent increase — `BruteWeight` arithmetic + over-discharge guard covered by `WeightRepoDischargeTests` (§12b); ticket labels covered by `PrintServiceLabelTests` (§12c). Both 2026-09-11.
- [x] 11c.4 Manual/regression test: a normal (non-pedido) weigh-in's `BruteWeight`/ticket labels are byte-for-byte the same as before this section — `BruteWeight` non-discharge path covered by `WeightRepoDischargeTests.Normal_entry_adds_loaded_weight_to_tare`/`UpdateAsync_on_a_normal_entry_is_unaffected_by_the_discharge_branch`; labels pinned to their pre-change text by `PrintServiceLabelTests` (`isDischarge: false` cases). §12b/§12c, 2026-09-11.
- [x] 11c.5 `grep -rn "RequiresDisTaring"` over `src/` (excl. migrations) shows only `PedidoLine`/`PedidoLineDto`/`PedidoLineRepo`/the Pedido-form MAUI files — zero hits in `WeightDetail`/`WeightDetailDto`/`PedidoService`'s convert path/the convert popup

## 12. Close the automatable §8/§11c verification gaps (explore pass, 2026-09-11)

The automated-test-foundations suite (2026-09-06) closed §8.2/§8.9-server incidentally without anyone reconciling this checklist against it. This section closes what's left that doesn't require a real DB/ERP/MAUI, so only genuinely owner-gated items remain (§8.5, §8.8, §9c.5, §10c.5, §10d.2, §11c.2).

### 12a. `Concluded` / `Close` semantics (closes §8.3, §8.4)
- [x] 12a.1 New `BasculaTerminalTest/Unit/Application/PedidoDtoTests.cs`: `PedidoLineDto.Concluded` is true when `ManuallyClosed=true` regardless of pending; true when pending reaches exactly 0 (and when it overshoots); false otherwise
- [x] 12a.2 Same file: `PedidoDto.Concluded` is false with zero lines, false while any line is unconcluded, true only once every line is concluded (mix of natural-zero-pending and manually-closed lines)

### 12b. `WeightRepo` discharge arithmetic (closes §11c.3, §11c.4's BruteWeight half, contributes to §8.7)
- [x] 12b.1 Added `Microsoft.EntityFrameworkCore.InMemory` 9.0.9 (version-matched to `Microsoft.EntityFrameworkCore.SqlServer`) to `BasculaTerminalTest.csproj`
- [x] 12b.2 New `BasculaTerminalTest/Unit/Infrastructure/WeightRepoDischargeTests.cs`: `RecomputeBruteWeightAsync`/`MarkDetailLoadedAsync`/`UpdateAsync` compute `TareWeight - Σloaded` when `IsDischarge`, `TareWeight + Σloaded` otherwise; over-discharge throws `InvalidOperationException`; unloaded/deleted details excluded. **Each test seeds and exercises through separate `WeightDBContext` instances on the same InMemory database name** — a single shared context let change-tracker relationship fixup silently bypass the repo's filtered `!IsDeleted` includes during a first draft (caught by `Unloaded_and_deleted_details_are_excluded_from_the_running_total` failing); the fix also better mirrors the real per-request scoped context.

### 12c. `PrintService` ticket labels (closes §11c.4's label half)
- [x] 12c.1 Extracted the two `entry.IsDischarge ? "..." : "..."` ternaries (`PrintService.cs`) into `internal static GetFinalWeightLabel`/`GetInitialWeightLabel` on `PrintService` — behavior-preserving, `BuildWeightDetailsTable`/`BuildWeightHeader` just call them
- [x] 12c.2 `<InternalsVisibleTo Include="BasculaTerminalTest" />` added to `Infrastructure.csproj` (none existed before)
- [x] 12c.3 New `BasculaTerminalTest/Unit/Infrastructure/PrintServiceLabelTests.cs`: both helpers return the discharge-labeled string when `true` and the original pre-change string when `false`

### 12d. Full partial-fulfillment loop (closes §8.1)
- [x] 12d.1 Extended `PedidoPartialFulfillmentFlowTests` with `Converting_a_line_twice_reaches_exactly_zero_pending_and_blocks_a_third_conversion`: converts 60 then the remaining 40 of a 100 line, asserts `PendingAmount == 0` and `Concluded == true`, then asserts a third conversion attempt is rejected with 400

### 12e. Bookkeeping
- [x] 12e.1 `dotnet test` on `BasculaInterface.CI.slnf --filter "Category!=Live"`: 67/67 green (60 unit + 7 integration), 0 warn/0 err build. Unit tier alone (`Category!=Integration&Category!=Live`, no Docker/Podman) also verified green on its own — the new InMemory-based tests stay runnable without a container engine, as intended.
- [x] 12e.2 §8.1/§8.3/§8.4/§8.7/§11c.3/§11c.4 re-checked above, each citing the new test name(s)
