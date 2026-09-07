# Design: Pedidos Remake — Multi-Product, Partial Fulfillment

## Context

`ProviderPurchase` (`Core.Domain/Entities/ProviderOrders/ProviderPurchase.cs`) is a flat row: `ProviderId`, `ProductId`, `RequiredAmount`, `RealAmount`, and a **singular, nullable** `WeightEntryId`. `ProviderPurchaseService.CreateWeightEntryAsync` (`Infrastructure/Service/ProviderPurchaseService.cs:59-103`) creates exactly one `WeightEntry` with exactly one `WeightDetail` for the purchase's product, sets `purchase.WeightEntryId`, and **throws `InvalidOperationException`** on any second attempt. There is no header entity grouping several products into one order — "pedido" today just means "one of these rows."

Separately: `ProductoDto` (`Core.Application/DTOs/ProductoDto.cs:25-36`) hardcodes almacén `"1"/"2"/"3"` from the product's contpaqi classification code (`CIDVALORCLASIFICACION6`), with no catalog behind those numbers anywhere in the codebase — `CIDALMACEN` only ever appears as a bare int/string column on other contpaqi mirror tables (`Movimiento`, `ClienteProveedor`) and on `ExternalTargetBehavior.TargetAlmacen` (also a raw string). `WeightService`'s document-building path (`Infrastructure/Service/WeightService.cs:285`) already resolves the ERP almacén per detail as `product.IdAlmacen ?? entry.ExternalTargetBehavior.TargetAlmacen` — i.e. product-first, entry-level-fallback — so per-detail almacén resolution is an existing pattern, not a new one.

Dis-taring exists only as `WeightDetail.SecondaryTare` + `WeightService.SetSecondaryTareAsync(detailId, tare)` — a manual value-setter with no config driving *when* it should be used.

This design was scoped via `enrich-issue-for-openspec` against issue #116; the six decisions below originated in that enrichment (marked **Confirmed during enrichment**). Decisions 5 and 6 were revisited with the project owner during a later explore pass and now diverge from the enrichment — the revised choices and their rationale are recorded in place.

## Goals / Non-Goals

**Goals:**
- Group multiple product lines under one `Pedido` header.
- Let any `PedidoLine` be converted to weight repeatedly, in any portion, until fully received.
- Let the operator pick an almacén target (a hidden `ExternalTargetBehavior`) at weigh-in — an explicit choice every time, pre-filled from the product's default.
- Let the operator declare, per pedido line, which products need dis-tare mode, and carry that onto each resulting weight detail for the weighing screen.
- Keep all of the above logic backend-side.

**Non-Goals:** contpaqi inventory changes; core weighing-capture redesign; sales-side pedidos; mirroring contpaqi's real almacén table; a multi-value weight-detail-type enum; migrating existing `ProviderPurchase` data; `BasculaUi` (React) changes.

## Decisions

### Decision 1: `Pedido` header + `PedidoLine` entities, replacing `ProviderPurchase` outright

**Chosen:** New entities under `Core.Domain/Entities/ProviderOrders/`:
```csharp
public class Pedido : BaseEntity {
    public required int ProviderId { get; set; }
    public DateTime ExpectedArrival { get; set; } = DateTime.Now.AddDays(7);
    public string? Notes { get; set; }
    public ICollection<PedidoLine> Lines { get; set; } = [];
}

public class PedidoLine : BaseEntity {
    public required int PedidoId { get; set; }
    public required int ProductId { get; set; }
    public required decimal RequiredAmount { get; set; }
    public decimal? Price { get; set; }
    public string? Notes { get; set; }
    public bool ManuallyClosed { get; set; } = false;   // see Decision 4
    public DateTime? LastUpdated { get; set; }
    [ForeignKey(nameof(PedidoId))] public virtual Pedido Pedido { get; set; } = null!;
}
```
`ProviderPurchase.cs`, `IProviderPurchaseRepo`/`ProviderPurchaseRepo`, `IProviderPurchaseService`/`ProviderPurchaseService`, `ProviderPurchaseController`, `ProviderPurchaseDto` are all deleted, replaced by `Pedido`/`PedidoLine` equivalents.

**Confirmed during enrichment:** header+lines model (not a flat grouping key on the existing table); clean replacement, no migration of existing rows.

**Rejected — add an `OrderId` grouping column to the existing flat `ProviderPurchase` table:** smaller diff, but leaves "pedido" as an implicit convention forever and doesn't fix the deeper 1:1-weighing problem, which needs its own entity change regardless (Decision 2). Doing both changes to the same overloaded entity is harder to reason about than replacing it.

### Decision 2: Received/pending amount computed from `WeightDetail`, not stored on `PedidoLine`

**Chosen:** `WeightDetail` gains `public int? FK_PedidoLineId { get; set; }`. A line's received amount is `Σ(WeightDetail.Weight WHERE FK_PedidoLineId == line.Id AND IsLoaded AND !IsDeleted)`; pending = `RequiredAmount - received`. Computed at query time in `PedidoLineDto` (and via a repo method for line lookups), never persisted.

**Confirmed during enrichment:** multiple `WeightEntry`s per line, each a partial contribution.

**Why the FK sits on `WeightDetail`, not `WeightEntry`:** a `WeightEntry` is one truck visit and can carry several products (`WeightDetails`) — only the product-specific detail row is meaningfully "this pedido line's weight." Putting the FK on `WeightEntry` would wrongly imply a whole truck visit belongs to one line.

**Rejected — a stored `ReceivedAmount` counter on `PedidoLine`, incremented on each weigh-in:** this is exactly the two-step-mutation risk class already known from `BruteWeight` (see `weight-detail-delete` design.md Decision 5: recompute must run after the write or it reads stale data). A computed sum has no second `SaveChangesAsync` to lose sync with, at the cost of a join at read time — an acceptable trade given pedido line counts are small.

### Decision 3: Converting a line to weight creates a new `WeightDetail`, optionally attached to an existing open `WeightEntry`

**Chosen:** `POST /api/Pedido/Line/{lineId}/ConvertToWeight` with body `{ WeightEntryId?: int, TargetAmount?: decimal, AlmacenId?: int, ExternalTarget?: string }`:
- If `WeightEntryId` is supplied, the new `WeightDetail` (`FK_PedidoLineId = lineId`, `RequiredAmount = TargetAmount ?? pending`) is appended to that entry (matching "Solo Pedidos" terminal's existing pattern of adding product slots to an open entry). Otherwise a new `WeightEntry` is created (`PartnerId = pedido.ProviderId`), mirroring today's `CreateWeightEntryAsync`.
- Rejects (`InvalidOperationException`) if `TargetAmount > pending` or the line is already closed (Decision 4).
- `TargetAmount` defaulting to the *full* pending amount reproduces "weigh all of this product"; passing a smaller value reproduces "only take a portion" — both explicitly requested in the issue.

**Rejected — require `TargetAmount` always:** would break the common "weigh what pending says" case with needless friction; defaulting to full pending covers it directly.

### Decision 4: Line/header conclusion — computed-fulfilled by default, plus an explicit manual force-close

**Chosen:** `PedidoLineDto.Concluded => pending <= 0 || ManuallyClosed`. `PedidoDto.Concluded => Lines.All(l => l.Concluded)`. Both computed, no stored header flag. A new `PATCH /api/Pedido/Line/{id}/Close` sets `ManuallyClosed = true` for the short-shipment case (provider will never deliver the rest).

**Confirmed during enrichment (resolving open question "header completion rule"):** natural fulfillment closes automatically; a manual override exists for accepting a short/cancelled shipment, mirroring the existing `ProviderPurchase.Concluded` flag's manual-set pattern (`ConcludeByWeightEntryAsync`) rather than inventing a new mechanism.

### Decision 5: Almacén target = a hidden `ExternalTargetBehavior`, picked at conversion (no separate `Almacen` catalog)

**Chosen:** There is **no `Almacen` entity**. The almacén target for a pedido conversion is one of the existing **hidden `ExternalTargetBehavior` rows** — the same mechanism the old `ProviderPurchase` flow used, which defaulted every provider purchase to a single hidden behavior (`CP26` / "COMPRA PRODUCCION"). Instead of one fixed default, the operator now **picks** among the hidden behaviors in the convert-to-weight dialog, and the pick becomes the new `WeightEntry`'s `ExternalTargetBehaviorFK` — driving the ERP document's Serie, Concepto **and** almacén (`TargetAlmacen`) in one shot.

`ExternalTargetBehavior` gains one column: **`string? AlmacenName`** — a human label for `TargetAlmacen`, shown in the picker as `"{TargetAlmacen} - {AlmacenName}"`. Non-hidden behaviors (the normal document-type picker: `PA26` "PEDIDO A GRANEL", `PE26` "PEDIDO ENCOSTALADO") are unaffected.

**Confirmed with project owner (revised after first implementation):**
- **The `Almacen` catalog is deleted** — entity, `IAlmacenRepo`/`AlmacenRepo`, `IAlmacenService`/`AlmacenService`, `AlmacenDto`, `AlmacenController`, its `DbSet`, and `WeightDetail.FK_AlmacenId` + the `Almacen` nav. The owner does not want a second almacén relation to own when this entity already exists and already carries `TargetAlmacen`.
- **The picker lists `ExternalTargetBehavior` rows where `Hidden == true`**, via `GET /api/ExternalTargetBehavior/AlmacenTargets`. The owner seeds the extra rows manually (all "COMPRA PRODUCCION", same `TargetConcept`, differing only in `TargetAlmacen` + `AlmacenName`).
- **Operator MUST pick one — no silent default.** The dialog pre-selects the row whose `TargetAlmacen` equals the product's classification-derived default (`ProductoDto.IdAlmacen`) when one matches, but always forces confirmation. If `AlmacenTargets` is empty the client raises an error and does not submit. Server-side, `ConvertToWeight` for a *new* weight entry rejects (`InvalidOperationException`) a missing or non-hidden `ExternalTarget`.
- **Per-conversion only, and per weight entry.** The pick lands on the new `WeightEntry.ExternalTargetBehaviorFK`; appending a line to an *existing* open entry (the "Solo Pedidos" add-product path) inherits that entry's already-fixed target and ignores the parameter.

**ERP resolution** (`WeightService.BuildContpaqiDocumentDto`) reverts to its pre-change form: `CodigoAlmacen = product.IdAlmacen ?? weightEntry.ExternalTargetBehavior.TargetAlmacen`. For a granel product with a hardcoded `IdAlmacen` (clasif6 7/8/9) that still wins — the existing, working magic-number mechanism is untouched; for everything else the operator's picked hidden behaviour supplies the almacén.

**Rejected — the local `Almacen(Id, Code, Name)` catalog (the first implementation):** a new table the owner would have to seed and keep coherent with the ERP, duplicating information (`TargetAlmacen`) that `ExternalTargetBehavior` already holds and that already flows into document submission. Reusing the existing entity means one relation, one seed step, one code path.

### Decision 6: Dis-tare is an operator-declared boolean on the pedido line and its weight details

**Chosen:** `PedidoLine` gains `public bool RequiresDisTaring { get; set; } = false;` (stored). `WeightDetail` gains `public bool RequiresDisTaring { get; set; } = false;` (stored). The operator declares — per pedido line, typically when building or editing the order — which products need dis-taring. When a line is converted to weight, the new `WeightDetail` inherits the line's current value; the `ConvertToWeight` UI shows it pre-checked from the line and lets the operator adjust it for that one conversion. The weighing screen reads `WeightDetail.RequiresDisTaring`.

No product-level storage, no ERP classification field: there is no `ProductWeighingConfig` table.

**Supersedes the enrichment-confirmed choice** ("per-product flag keyed by `CIDPRODUCTO` in a local `ProductWeighingConfig` table"). Reversed deliberately by the project owner after initial implementation, for three reasons: (1) unwillingness to own and maintain a product↔config relation; (2) the alternative of repurposing a spare `Producto.CIDVALORCLASIFICACION*` field risks disturbing an ERP configuration whose full blast radius isn't known ("don't want to break prod"); (3) the existing, working `CIDVALORCLASIFICACION6` magic-number mechanism (drives `IsGranel` + the almacén default in Decision 5) stays completely untouched.

**Consequences:** `ProductWeighingConfig` entity + `IProductWeighingConfigRepo`/`ProductWeighingConfigRepo` + its `DbSet` + DI registration are deleted; `ProductService` drops the `IProductWeighingConfigRepo` dependency and `HydrateDisTaringAsync`; `ProductoDto.RequiresDisTaring` is removed (no source, no consumer); `PedidoService` drops the same dependency and its two hydration helpers — `PedidoLineDto.RequiresDisTaring` now maps straight from the stored entity field. Net: one fewer table, one fewer repo, less service code than the initial implementation.

**Trade-off accepted:** no authoritative system-wide "these products always need dis-taring" list; the operator re-declares per pedido line (one checkbox; a line is one product). If a product-level default is ever wanted, it can layer on top of these columns later without disturbing them.

**Still open (frontend/UX, not backend):** whether the weighing screen *auto-invokes* `SetSecondaryTareAsync` behavior when `WeightDetail.RequiresDisTaring` is true, or merely *displays* the signal for the operator to act on. The backend guarantee is only that the flag is carried on line and detail. Non-pedido weigh-ins (no line) always get `false`; the manual `SetSecondaryTareAsync` path still covers those. See Open Questions.

**Reverted (2026-09-04):** the owner confirmed exploration showing `WeightDetail.RequiresDisTaring` never actually drove anything — `WeightingScreen`/`BasculaViewModel`/`WeightService` never read it. Rolled back: `WeightDetail` no longer has this column at all; `PedidoLine.RequiresDisTaring` stays as a plain informational note on the order line, and the pre-existing manual `SecondaryTare`/`SetSecondaryTareAsync` mechanism is completely untouched, exactly as it worked before this change. See the rewritten `product-dis-tare-signal` spec and Decision 7 below (a real, separate bug the same conversation surfaced).

### Decision 7: `WeightEntry.IsDischarge` — pedido deliveries run the scale backwards

**Chosen:** Every weighing path assumed the first reading on a `WeightEntry` is the empty-vehicle tare, and every later reading only *adds* to a running `BruteWeight` (`WeightRepo.RecomputeBruteWeightAsync`/`MarkDetailLoadedAsync`/`UpdateAsync`, all duplicating `TareWeight + Σ(loaded detail.Weight)`). That's backwards for a pedido delivery: the truck arrives **full**, discharges into the almacén, and leaves **lighter**. The per-product `Math.Abs` diff capture in `BasculaViewModel` already produces the correct discharged amount regardless of direction — the bug is purely in the running total's sign.

`WeightEntry` gains `bool IsDischarge` (default `false`), automatically set `true` on the new entry `PedidoService.ConvertLineToWeightAsync` creates (pedidos are always provider deliveries; the "append to existing open entry" branch inherits whatever the entry already has, same as the almacén target). `WeightRepo`'s three `BruteWeight` formulas branch on it: `TareWeight - Σ(loaded)` when discharging, `TareWeight + Σ(loaded)` otherwise — with a guard against discharging more than `TareWeight` (`InvalidOperationException`). `DetailedWeightViewModel.TotalWeight` (the MAUI live-weighing total) gets the same branch so the in-app number doesn't climb while the server-side number correctly falls.

`PrintService`'s ticket reads `entry.IsDischarge` directly for its two direction-dependent labels (`"TARA INICIAL:"`/`"BRUTO:"` → `"PESO INICIAL (CARGADO):"`/`"PESO FINAL (VACÍO):"`) instead of leaving the direction implicit — this replaces the only place printing already inferred anything indirectly (`partner.IsProvider ? "Proveedor:" : "Socio:"`, which stays as-is since it's about partner identity, not visit direction) with the real, direct signal. No other ticket content changes; once `BruteWeight` computes correctly the numbers alone already stop reading as "the vehicle left heavier."

**Confirmed with project owner (feedback during the same change's rollout, 2026-09-03/04):** "pedidos are the other way around, the vehicle arrives full, discharges, and weighs less... we need to capture our absolute difference, that works fine, but we need to give a ticket to the provider vehicle, and that ticket needs to have... the weight of the vehicle empty, and the ticket will [not] show an increase, suggesting the vehicle is leaving heavier." Direction is automatic-from-Pedido only (no manual toggle elsewhere) — confirmed as the recommended, lowest-effort option.

**Rejected — a manual toggle in the weighing screen for any entry:** would need new UI and an operator to remember to set it; automatic-from-Pedido covers the only scenario that currently exists (pedidos are always provider deliveries) with zero new UI.

## API Surface

```
New — PedidoController (replaces ProviderPurchaseController):
  POST   /api/Pedido                          create header + initial lines
  GET    /api/Pedido/{id}                     header + lines + computed received/pending
  GET    /api/Pedido/All?top=&page=
  GET    /api/Pedido/All/ByProvider?providerId=&top=&page=
  PUT    /api/Pedido                          update header fields
  DELETE /api/Pedido?id=                      soft-delete header (cascades to lines)
  POST   /api/Pedido/Line                     add a line to an existing header
  PUT    /api/Pedido/Line                     update a line (RequiredAmount, Price, Notes)
  DELETE /api/Pedido/Line?id=
  POST   /api/Pedido/Line/{id}/ConvertToWeight   body: { WeightEntryId?, TargetAmount?, ExternalTarget?, RequiresDisTaring? }
                                                 ExternalTarget = the picked hidden ExternalTargetBehavior id;
                                                 required (and must be Hidden) when creating a new weight entry (Decision 5)
  PATCH  /api/Pedido/Line/{id}/Close             manual force-close (Decision 4)

Extended — ExternalTargetBehaviorController:
  GET    /api/ExternalTargetBehavior/AlmacenTargets   the Hidden==true rows, for the pedido almacén picker;
                                                      client errors if it returns empty (Decision 5)

WeightService document-building: almacén resolution is unchanged from before this change —
  product.IdAlmacen ?? weightEntry.ExternalTargetBehavior.TargetAlmacen  (Decision 5)
```

## Migration Plan

Three migrations (the second because Decision 5 was revised after the first shipped; the third for the Decision 6 revert + Decision 7):
1. `20260903021916_PedidosRemake`: `DropTable(ProviderPurchases)`; `CreateTable(Pedidos)`; `CreateTable(PedidoLines)` (incl. `RequiresDisTaring bool NOT NULL DEFAULT false`); `CreateTable(Almacenes)`; `AlterTable(WeightDetails)` adding nullable `FK_PedidoLineId`, `FK_AlmacenId` (FKs `ON DELETE SET NULL`) and `RequiresDisTaring bool NOT NULL DEFAULT false`.
2. `..._DropAlmacenCatalogUseExternalTargetBehavior`: `DropTable(Almacenes)`; drop `WeightDetails.FK_AlmacenId` (+ its FK and index); `AddColumn(ExternalTargetBehaviors.AlmacenName text NULL)`. `PedidoLines.RequiresDisTaring` / `WeightDetails.RequiresDisTaring` are kept.
3. `..._DischargeDirectionAndDisTaringRollback`: `DropColumn(WeightDetails.RequiresDisTaring)`; `AddColumn(WeightEntries.IsDischarge boolean NOT NULL DEFAULT false)`. `PedidoLines.RequiresDisTaring` is kept.
4. Deploy API + all migrations together (schema-breaking on `ProviderPurchases`, but that table is confirmed to hold no data worth preserving). Owner seeds the extra hidden `ExternalTargetBehavior` rows (COMPRA PRODUCCION, differing `TargetAlmacen`/`AlmacenName`).
5. Deploy updated MAUI client (`ProviderPurchaseListView`/`FormView` → new Pedido views) in the same release — the old client can't function against the new API.
6. Rollback: restore the previous migration and redeploy the previous API/client build; no data-preservation concern either direction, per Decision 1.

## Risks / Trade-offs

- **Breaking schema change, no data migration** — accepted per explicit confirmation; only safe because there's no production data in `ProviderPurchases` worth carrying forward.
- **Computed pending/received amounts add a join on every line read** — accepted; line counts per pedido are small, and it eliminates an entire class of recompute-staleness bugs (Decision 2).
- **Client and API must deploy together** — the MAUI client has no graceful-degradation path against the old `ProviderPurchase` endpoints once they're removed; treated as a single coordinated release, same as any other breaking API change in this solo-developer project.
- **Almacén target is per weight entry, not per detail** (Decision 5) — a `WeightEntry` carries one `ExternalTargetBehaviorFK`, so two pedido lines converted into the *same* open entry share its almacén. Acceptable: each `ConvertToWeight` without a `WeightEntryId` makes its own entry with its own picked target, and the old 1:1 `ProviderPurchase` flow had no per-detail almacén either.
- **No product-level dis-taring list** (Decision 6) — the operator re-declares dis-taring per pedido line. Accepted trade for not owning a product↔config relation or touching ERP classification config.

## Open Questions

- ~~**Auto-invoking dis-tare behavior vs. merely signaling it**~~ — moot: `WeightDetail.RequiresDisTaring` was removed (Decision 6 revert); the weighing screen's manual `SecondaryTare` mechanism is untouched.
