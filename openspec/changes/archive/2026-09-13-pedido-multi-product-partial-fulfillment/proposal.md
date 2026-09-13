## Why

Issue #116 asks for a "Pedidos remake." The existing `ProviderPurchase` (`Core.Domain/Entities/ProviderOrders/ProviderPurchase.cs`) models a pedido as one flat row per `(provider, product, amount)`, with a **1:1** link to a `WeightEntry` — `ProviderPurchaseService.CreateWeightEntryAsync` throws once that link is set (`"...ya tiene una entrada de peso asignada..."`). That makes it impossible to represent how providers actually deliver: several products per order, arriving in installments across separate visits (e.g. 5,000 of 10,000 kg today, the rest later). Today's model also has no named almacén to target (`ProductoDto` hardcodes almacén 1/2/3 off the product's classification code, with no catalog behind those numbers) and no product-driven signal for when a weighing screen needs dis-taring — the only mechanism (`WeightDetail.SecondaryTare` / `WeightService.SetSecondaryTareAsync`) is a manual, config-less toggle.

This proposal was scoped through `enrich-issue-for-openspec` against issue #116, resolving six business decisions with the project owner (see design.md Decisions) before drafting.

## What Changes

- **New `Pedido` header + `PedidoLine` entities**, replacing `ProviderPurchase` entirely (clean replacement — no data migration; confirmed no production data needs to survive). One pedido groups multiple product lines under one provider.
- **Partial, repeated fulfillment per line**: `WeightDetail` gains a nullable `FK_PedidoLineId`, so a line accumulates weight across any number of `WeightDetail`s (across one or many `WeightEntry` visits) instead of exactly one. Received/pending amounts are computed on read (`RequiredAmount - Σ IsLoaded details.Weight`), not stored counters — avoiding the two-step recompute staleness risk already known from the `BruteWeight` mutation path.
- **Almacén target via hidden `ExternalTargetBehavior`**: no new almacén entity. The almacén target is one of the existing hidden `ExternalTargetBehavior` rows — the same mechanism the old `ProviderPurchase` flow defaulted to. `ExternalTargetBehavior` gains an `AlmacenName` label column; `GET /api/ExternalTargetBehavior/AlmacenTargets` lists the hidden rows. The operator MUST pick one every time a line is converted to a new weight entry — pre-filled from the product's default `TargetAlmacen` match, always confirmed; an empty list raises a configuration error. The pick becomes the `WeightEntry.ExternalTargetBehaviorFK`, driving the ERP document's Serie/Concepto/almacén. ERP almacén resolution stays `product.IdAlmacen ?? entry.ExternalTargetBehavior.TargetAlmacen` (unchanged from before this change).
- **Operator-declared dis-taring note**: a stored `RequiresDisTaring` boolean on `PedidoLine` only (set by the operator when building the order), purely informational — shown as a badge/checkbox on the order. **Reverted (2026-09-04):** an earlier pass also stored this on `WeightDetail`, inherited at conversion; the owner confirmed it never drove any weighing-screen behavior and rolled it back. The pre-existing manual `WeightDetail.SecondaryTare`/`WeightService.SetSecondaryTareAsync` mechanism is untouched, exactly as it worked before this change.
- **Discharge-direction fix for pedido deliveries**: a stored `IsDischarge` boolean on `WeightEntry`, automatically `true` for entries created via pedido conversion (a delivery arrives loaded and unloads, rather than arriving empty and being loaded). `WeightRepo`'s running `BruteWeight` now counts down from `TareWeight` for a discharge entry instead of always adding — fixing a real bug where a pedido delivery's ticket showed the vehicle "gaining" weight. The printed ticket's two direction-dependent labels read this flag directly instead of leaving direction implicit.
- **Backend-owned business logic**: all of the above (pending calculation, almacén defaulting/override, discharge-direction weight math, line/header conclusion) lives in `Core.Application`/`Infrastructure`/`BasculaTerminalApi`. The MAUI client (`BasculaInterface`) only calls the new endpoints and renders their results — consistent with how #121/#122/#124/#125 were implemented.

## Capabilities

### New Capabilities
- `pedido-header-lines`: A `Pedido` header groups multiple `PedidoLine`s (one per product), replacing the flat `ProviderPurchase` model.
- `pedido-partial-fulfillment`: A `PedidoLine` can be converted to weight repeatedly, in any portion, across separate `WeightEntry` visits, with computed received/pending amounts and both automatic and manual line conclusion.
- `almacen-target-selection`: A mandatory operator pick, at weigh-in, of a hidden `ExternalTargetBehavior` (labelled by a new `AlmacenName` column) — pre-filled from the product default, always confirmed, error on empty list — becoming the weight entry's `ExternalTargetBehaviorFK`. No new almacén entity; ERP resolution unchanged.
- `product-dis-tare-signal`: An operator-declared `RequiresDisTaring` boolean on each pedido line — informational only; it does not reach `WeightDetail` or the weighing screen (reverted from an earlier pass).
- `weight-entry-discharge-direction`: A `WeightEntry.IsDischarge` boolean, automatic for pedido-created entries, that flips the running `BruteWeight` computation and the printed ticket's direction-dependent labels for a delivery that arrives loaded and discharges.

### Removed Capabilities
- The implicit single-weighing `ProviderPurchase` model (entity, repo, service, controller, DTO) is deleted outright, not deprecated in place.

## Non-goals

- Any change to contpaqi's own inventory handling — the ERP still owns inventory once a document posts.
- Redesigning the core weighing capture flow (tare, brute weight, `WeightDetail` mutations) beyond honoring the dis-tare signal and almacén override.
- Sales-side or non-provider pedidos.
- Any new almacén entity — the almacén target reuses the existing `ExternalTargetBehavior` (hidden rows), owner-seeded (confirmed decision, revised from an earlier `Almacen` catalog).
- Any product-level dis-taring configuration, or repurposing contpaqi's `CIDVALORCLASIFICACION*` fields — the dis-tare declaration is per pedido line, entered by the operator (design.md Decision 6).
- A multi-value "weight detail type" enum — `RequiresDisTaring` ships as a boolean; a richer type is deferred.
- Auto-invoking dis-tare behavior in the weighing screen — this change only delivers the signal; whether the screen acts on it automatically is a later frontend decision (task 7.4).
- Migrating existing `ProviderPurchase` rows — confirmed clean replacement.
- React admin frontend (`BasculaUi`) changes — MAUI (`BasculaInterface`) only, consistent with prior pedido/weight-detail changes.

## Impact

**Affected terminals:** Main and "Solo Pedidos" terminals (where pedido conversion and weighing happen); Secondary terminal unaffected.
**API:** New `PedidoController` (replaces `ProviderPurchaseController`); `WeightController`'s document-building path gains almacén-override awareness.
**Domain/Repo/Service:** New `Pedido`, `PedidoLine` entities and repos/services; `ProviderPurchase` and its repo/service/controller/DTO are removed. Neither `ProductWeighingConfig` nor an `Almacen` entity is part of the final design — dis-taring lives on `PedidoLine`/`WeightDetail`, and the almacén target reuses hidden `ExternalTargetBehavior` (which gains an `AlmacenName` column).
**Database:** Three migrations — (1) drop `ProviderPurchases`; create `Pedidos`, `PedidoLines`; alter `WeightDetails` (add `FK_PedidoLineId`, `RequiresDisTaring`); (2) `AddColumn ExternalTargetBehaviors.AlmacenName`; (3) drop `WeightDetails.RequiresDisTaring`, add `WeightEntries.IsDischarge`. (The interim `Almacenes` table and `WeightDetails.FK_AlmacenId` from an earlier revision are created then dropped.)
**Client:** `BasculaInterface` — `ProviderPurchaseListView`/`FormView` and their ViewModels reworked as thin orchestration over the new endpoints.
