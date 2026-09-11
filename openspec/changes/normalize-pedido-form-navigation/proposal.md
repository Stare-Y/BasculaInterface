## Why

Issue #129 reports that `PedidoFormView` (the Pedido create/edit screen) doesn't match the rest of the app: there is no back button — leaving the screen means clicking "Cancelar", which sits in the same button row as "Guardar"/"Eliminar" — and the screen's overall element structure "feels unnatural and unharmonic" compared to the rest of the UI.

Checked against the repo, the complaint holds up structurally: every comparable screen (`PedidoListView`, `DetailedWeightView`, `PartnerSelectView`, `ProductSelectView`, `PendingWeightsView`, `FinishedWeights`) uses a responsive WinUI side-panel / Android bottom-bar chrome with a dedicated, clearly-labeled back button. `PedidoFormView` is a single flat, non-responsive `ScrollView` with no back button at all — "Cancelar" is the only way out, and it reads as a form-cancel action, not page navigation.

This proposal was scoped through `enrich-issue-for-openspec` against issue #129, resolving two scope-defining decisions with the project owner (see design.md Decisions).

## What Changes

- **`PedidoFormView` gets a dedicated back action** ("Regresar"), replacing "Cancelar" outright — same navigate-back behavior as today (`Shell.Current.Navigation.PopAsync()`, no confirmation/save prompt), just correctly positioned and labeled.
- **Full chrome normalization**: `PedidoFormView`'s layout is rebuilt to match `PedidoListView`'s established responsive pattern — a WinUI side panel and an Android bottom bar, each holding the same three actions (`Guardar`, `Eliminar` when visible, `Regresar`) — instead of today's flat, single-column `ScrollView`.
- **All existing screen content and behavior is preserved as-is** inside the new content region: Provider/Fecha Esperada/Notas sections, the `Concluded` read-only state handling, the Lines `CollectionView`, the "Agregar Producto" panel, and the convert-to-weight / close-line flows and popups.
- **Automation hooks for the existing VM bot harness**: `AutomationProperties.AutomationId` added to the restructured buttons, continuing the `AutomationId` coverage follow-up already flagged in `automated-test-foundations`, plus a small FlaUI smoke-test extension asserting the new back button exists and works.
- **Manual owner sign-off before archiving**: because this repo's Linux dev environment cannot build or run the MAUI client (a standing limitation noted in every prior MAUI-touching change here), the change's tasks end with publishing to `win10-maui-dev`, running the bot smoke test, and then asking the project owner to click through the restructured screen and approve it — or request further UI adjustments — before the change is archived.

## Capabilities

### New Capabilities
- `pedido-form-navigation`: `PedidoFormView` exposes a single, conventionally-placed back action and a responsive WinUI/Android chrome consistent with the rest of the app, replacing the previous flat layout's bottom-row "Cancelar" button.

## Non-goals

- Any backend/API/database change — this is a `BasculaInterface` (MAUI) presentation-layer change only.
- Any change to save/delete/add-line/convert-line/close-line business behavior — only their position within the new layout changes.
- An unsaved-changes confirmation prompt on back — explicitly rejected; back keeps today's no-prompt behavior.
- Normalizing any other screen — this change targets `PedidoFormView` only.
- Broad `AutomationId` coverage across the whole app (tracked separately in `automated-test-foundations` §5.1) — only the buttons this change touches get one.

## Impact

**Affected terminals:** Main and "Solo Pedidos" terminals (where the Pedido form is used); no other screen is touched.
**API:** none.
**Domain/Repo/Service:** none.
**Database:** none.
**Client:** `src/backend/BasculaInterface/Views/PedidoFormView.xaml` and `.xaml.cs` only. `PedidoFormViewModel` is unaffected (no ViewModel-level state needed for the chrome change).
**Tests:** `src/backend/BasculaBotTests` gains an `AutomationId`-driven assertion for the new back button; no unit/integration test changes (nothing backend-observable changes).
**Verification:** requires a human pass on `win10-maui-dev` (or the owner's dev machine) — Linux CI/sandbox cannot render or build the MAUI head.
