## Why

The manager-password gate from #122 already protects changing a weight detail's product/partner/amount and — since #125 — soft-deleting a captured `WeightDetail`. But three sibling deletes still bypass it entirely, guarded only by a plain yes/no `DisplayAlert`: deleting a whole `WeightEntry` (`DELETE api/Weight`, `DetailedWeightView.BtnDeleteEntry_Clicked`) and deleting a whole `Pedido` (`DELETE api/Pedido`, `PedidoFormView.BtnDelete_Clicked`). A fourth, `DELETE api/Pedido/Line`, is exposed with no gate at all and has no UI caller yet — a latent gap waiting for one. Issue #133 asks to harness the existing gate over all of these, not just the weight entry named in its title.

Unlike the weight-detail case, none of these four have a legitimate "safe to leave unguarded" exemption (there's no empty-row equivalent for a whole entry, a whole pedido, or a pedido line) — so the fix is to gate the *existing* endpoints in place, not add parallel guarded routes beside still-open unguarded ones.

## What Changes

- **`WeightEntry` delete** — `DELETE api/Weight?id=` is replaced by `PATCH api/Weight/{id}/Delete` (body: `{ PasswordHash }`), mirroring `Weight/Detail/{id}/Delete`'s shape. Blocks once `ConptaqiComercialFK > 0` (new — matches every sibling guarded mutation); still bypasses the `ConcludeDate` lock, same as the rest.
- **`Pedido` delete** — `DELETE api/Pedido?id=` is replaced by `PATCH api/Pedido/{id}/Delete` (body: `{ PasswordHash }`). Reuses the same shared password.
- **`PedidoLine` delete** — `DELETE api/Pedido/Line?id=` is replaced by `PATCH api/Pedido/Line/{id}/Delete` (body: `{ PasswordHash }`), gated even though nothing calls it yet, so the gap can't resurface silently when a caller is added later.
- **Password gate** — all three reuse the exact shared secret from #122/#125 (`WeightSettings.ChangeProductPasswordHash`, via `IOptions<WeightSettings>`); no new setting. `PedidoService` gains that dependency for the first time.
- **MAUI client** — `DetailedWeightView`'s and `PedidoFormView`'s delete buttons swap their plain `DisplayAlert` confirm for a password-entry popup (reusing `DeleteDetailPopUp`/`DeleteDetailConfirmPopUp`'s shape), and their ViewModels hash the password client-side before calling the new endpoints. `PedidoListViewModel.DeletePedidoAsync` (currently unused by any View) gets the same signature change so it isn't a latent gap.

## Capabilities

### New Capabilities
- `weight-entry-delete`: Password-gated whole-`WeightEntry` soft delete, blocked once an ERP document exists, bypassing the concluded-entry lock like its sibling guarded mutations.
- `pedido-delete`: Password-gated whole-`Pedido` soft delete (cascading to its lines, unchanged from today).
- `pedido-line-delete`: Password-gated `PedidoLine` soft delete — gated at the API/service layer ahead of any UI caller.

## Non-goals

- Building this flow in the React admin frontend (`BasculaUi`) — MAUI desktop (`BasculaInterface`) only, consistent with every prior guarded-mutation change.
- The unguarded, empty-row `DELETE api/Weight/Detail` and its "✕" button — untouched, per its own documented exemption.
- Any permanent/per-user authentication model — still the #122 stopgap shared password.
- Renaming `ChangeProductPasswordHash` to a scope-neutral name — deferred, same open question carried since #124/#125.
- Wiring `PedidoListViewModel.DeletePedidoAsync` into any View — it has no caller today and this change doesn't add one, only keeps its signature consistent with the gated endpoint.
- An audit trail beyond what already exists.

## Impact

**Affected terminals:** Main and "Solo Pedidos" terminals (where these delete buttons live); Secondary terminal is unaffected.
**API:** `WeightController` (`Delete` action replaced), `PedidoController` (`Delete` and `DeleteLine` actions replaced); `IWeightService`/`WeightService` (`DeleteAsync` → `DeleteSafelyAsync`); `IPedidoService`/`PedidoService` (`DeleteAsync`/`DeleteLineAsync` → password-gated, new `IOptions<WeightSettings>` dependency).
**Repo layer:** No changes — reuses existing `WeightRepo.DeleteAsync`, `PedidoRepo.DeleteAsync`, `PedidoLineRepo.DeleteAsync` as-is.
**Client:** `BasculaInterface` — `DetailedWeightView.xaml.cs`, `DetailedWeightViewModel.cs`, `PedidoFormView.xaml.cs`, `PedidoFormViewModel.cs`, `PedidoListViewModel.cs`; reuses/extends the existing `DeleteDetailPopUp` popup rather than building a new one per screen where its shape fits.
**Database:** No schema migration.
**Config:** None new — reuses `WeightSettings.ChangeProductPasswordHash`.
**Breaking:** The four replaced endpoints change their request contract (query-string `DELETE` → `PATCH` with a JSON body) and must ship API + MAUI client together, same as every prior guarded-mutation deploy.
