# Weight Flow — Baseline Spec

## Domain Overview

A truck visit ("entrada de peso") goes through a multi-terminal flow:

```
[Pedidos terminal]          [Secondary terminal]       [Main terminal]
create WeightEntry     →    weigh products on     →    conclude process
add product slots           secondary scale             Contpaqi, print
(empty WeightDetails)       mark each as loaded
```

## WeightEntry Lifecycle

```
CREATED (ConcludeDate=null)
  │
  ├─ TareWeight = 0, BruteWeight = 0 initially
  ├─ WeightDetails[] = product slots added by pedidos/secondary terminals
  │
  ├─ [Main terminal captures initial truck weight]
  │    TareWeight = scale reading (empty truck)
  │    BruteWeight = TareWeight + Σ(loaded detail weights)  ← virtual running total
  │
  ├─ [Secondary terminal weighs each product]
  │    detail.Weight = abs(full_tarima - empty_tarima)
  │    detail.IsLoaded → true (after physically loaded into truck)
  │    BruteWeight += detail.Weight
  │
  └─ CONCLUDED (ConcludeDate set)
       immutable after this point
       triggers ProviderPurchase.Concluded = true
       optionally posts to ContpaqiComercial
```

## WeightDetail Semantics

| Field         | When set                               | Meaning                                      |
|---------------|----------------------------------------|----------------------------------------------|
| Weight        | After secondary scale capture          | Net weight of product batch (kg)             |
| Tare          | At capture time                        | Truck BruteWeight at that moment             |
| SecondaryTare | After user sets initial tarima weight  | Weight of empty tarima/pallet                |
| RequiredAmount| At detail creation                     | Expected quantity (from purchase order)      |
| IsLoaded      | Starts FALSE; set TRUE after loading   | Whether this batch is physically in truck    |
| Costales      | Optional                               | Number of sacks in this batch                |

## BruteWeight Accumulation Rule

`WeightEntry.BruteWeight` is the source of truth for the running total.

**Intended formula:**
```
BruteWeight = TareWeight + Σ(details where IsLoaded=true).Weight
```

Server-owned and recomputed via `RecomputeBruteWeightAsync` on any mutation that changes loaded state. See `weight-detail-mutations` spec for the full mutation contract. Concurrency is protected via xmin OCC — see `weight-concurrency` spec.

## IsLoaded Lifecycle (secondary terminal flow)

```
AddProductToWeightEntry          PutSecondaryTara           CaptureNewWeightEntry     MarkAsLoaded_Clicked
detail created                   user sets empty            product weighed on        user taps ✓ button
IsLoaded=true (default)   →     tarima weight         →    secondary scale      →    SetWeightDetailLoaded
Weight=0                         IsLoaded=false             Weight=diff               IsLoaded=true
SecondaryTare=null               SecondaryTare=X            (BruteWeight+=diff)
[✓ button hidden]                [✓ button hidden]          [✓ button VISIBLE]        [✓ button hidden]
```

CheckMarkVisibilityConverter: button shows when `!IsLoaded && SecondaryTare != null && Weight != 0`

For main terminal flow (no secondary scale): IsLoaded stays true the whole time (default is correct).
For secondary terminal flow: IsLoaded goes true → false → true through the lifecycle above.

Entry point into secondary weighing flow:
- User TAPS a row in `CollectionViewWeightDetails` (SelectionChanged handler, NOT a button)
- Conditions: `row.Tare == 0 && row.Weight == 0 && row.IsGranel && !OnlyPedidos`
- Opens `WeightingScreen` with `useIncommingTara: false`, `targetWeightDetail: row.Id`
- This is the `CollectionViewWeightDetails_SelectionChanged` handler in `DetailedWeightView.xaml.cs:645`

BruteWeight role at conclusion:
- After all-but-last products are weighed on secondary scale, the truck goes to the main scale
- Main scale reading - TareWeight - Σ(previous loaded products) = weight of last product
- This is the "des-tare" final confirmation; BruteWeight at that point = theoretical total truck weight

## Related Specs

- [`weight-concurrency`](../weight-concurrency/spec.md) — xmin OCC and MAUI retry on 409
- [`weight-detail-mutations`](../weight-detail-mutations/spec.md) — narrow endpoints for detail mutations and BruteWeight ownership
- [`weight-entry-conclude`](../weight-entry-conclude/spec.md) — conclude endpoint and downstream triggers

## Terminal Configuration (MAUI Preferences)

| Preference         | Effect                                               |
|--------------------|------------------------------------------------------|
| `SecondaryTerminal`| Hides new-entry/delete/print buttons; shows weighing |
| `OnlyPedidos`      | Shows add-product, hides new-entry                   |
| `BypasTurn`        | Skips the turn/queue system                          |
| `ManualWeight`     | Shows manual weight entry field                      |
