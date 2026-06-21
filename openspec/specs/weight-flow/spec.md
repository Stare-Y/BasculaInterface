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

This is currently computed client-side in `BasculaViewModel.CaptureNewWeightEntry` and
written via `PUT /api/Weight` (full DTO). No server-side enforcement or concurrency protection exists yet.

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

## Known Issues (as of 2026-06-21)

### 1. IsLoaded defaults to true everywhere
- `WeightDetail.cs`: `IsLoaded = true`
- `WeightDetailDto.cs`: `IsLoaded = true`
- DB migration `IsLoadedFlagOnDetail`: `defaultValue: true`
- Result: new empty details are born as "loaded"; `SetWeightDetailLoaded` is a flag no-op
- Intent: should start `false`, only become `true` after physical loading

### 2. BruteWeight computed client-side, no server protection
- `BasculaViewModel.CaptureNewWeightEntry`: `BruteWeight += _diferenciaAbs`
- `WeightRepo.UpdateAsync`: `existingEntry.BruteWeight = weightEntry.BruteWeight` (blind overwrite)
- No optimistic concurrency check (no row version, no LastUpdated guard on WeightEntry)
- Result: concurrent terminal writes cause lost updates on BruteWeight

### 3. PUT /api/Weight is the only mutation path (too wide)
- All mutations — notes, partner, IsLoaded toggle, BruteWeight update — go through one endpoint
- `SetWeightDetailLoaded` sends full WeightEntry snapshot; stale BruteWeight can overwrite correct DB value
- Needed: dedicated narrow endpoints per mutation type

### 4. No record-locking between terminals
- Multiple terminals can read and write the same WeightEntry simultaneously
- The `CanWeight` / `ReleaseWeight` mechanism exists only for the physical scale socket,
  not for WeightEntry record access
- Possible approaches: optimistic concurrency (EF RowVersion), or application-level record locking

## Terminal Configuration (MAUI Preferences)

| Preference         | Effect                                               |
|--------------------|------------------------------------------------------|
| `SecondaryTerminal`| Hides new-entry/delete/print buttons; shows weighing |
| `OnlyPedidos`      | Shows add-product, hides new-entry                   |
| `BypasTurn`        | Skips the turn/queue system                          |
| `ManualWeight`     | Shows manual weight entry field                      |
