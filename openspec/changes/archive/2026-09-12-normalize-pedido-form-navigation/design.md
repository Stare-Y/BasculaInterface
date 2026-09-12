# Design: Normalize Pedido Form Navigation

## Context

`PedidoFormView` (`src/backend/BasculaInterface/Views/PedidoFormView.xaml`) is the create/edit screen for a `Pedido` header + its `PedidoLine`s, introduced in `pedido-multi-product-partial-fulfillment` as a straight port of the old `ProviderPurchaseFormView` shape — a single `ScrollView` of stacked `Border` sections, with `Guardar`/`Eliminar`/`Cancelar` as one 3-column button row at the bottom of the header section.

Every other screen in the app instead uses a responsive chrome: a `Grid` with `OnPlatform`-driven `RowDefinitions`/`ColumnDefinitions` that renders a WinUI **side panel** (primary actions, `RoundRectangle 30` `Border`, `DefaultShadow`) next to a scrollable content column, and an Android **bottom bar** (the same actions as a `Grid` of text buttons) below a full-width content area. `PedidoListView` — this screen's direct parent/sibling in the same feature — is the clearest reference: its side panel holds `Nuevo Pedido` / `Actualizar` / `Regresar`, and its Android bottom bar mirrors the same three as `BtnCreateAndroid` / `BtnRefreshAndroid` / `BtnBackAndroid`, all wired to the same click handlers as the WinUI versions.

`PedidoFormView` never adopted this pattern, which is what issue #129 flagged: no back button, and a bottom-row "Cancelar" that reads as a form action rather than navigation.

This repo's Linux dev/CI environment cannot build or render the MAUI head (`net8.0-windows`/no Android workload since `automated-test-foundations`) — every prior MAUI-touching change in this repo's history has shipped without a Linux build/smoke check, verified instead on `win10-maui-dev`. The VM bot harness (FlaUI, `BasculaBotTests`) from `automated-test-foundations` exists for exactly this gap; this change is small enough to extend it with one assertion rather than requiring net-new bot infrastructure.

## Goals / Non-Goals

**Goals:** `PedidoFormView` gets one conventionally-placed, conventionally-labeled back action; its chrome matches `PedidoListView`'s responsive WinUI-panel/Android-bar pattern; all existing header/lines functionality is preserved untouched inside the new content region; the new back button is reachable by the VM bot harness; the owner explicitly signs off on the rendered result before this change is archived.

**Non-Goals:** any backend change; any behavioral change to save/delete/add-line/convert-line/close-line; an unsaved-changes prompt on back; normalizing any other screen; broad `AutomationId` coverage beyond this screen's touched controls.

## Decisions

### Decision 1: Cancel and Back collapse into one action (owner-confirmed)
The standalone `BtnCancel` ("Cancelar") is removed outright rather than kept alongside a new back button. The new back button (`BtnBack`, text "Regresar") is the only way to leave the page and keeps exactly today's behavior — `await Shell.Current.Navigation.PopAsync();`, no confirmation, no discard prompt. This matches how `Cancelar` already behaves today (`BtnCancel_Clicked` is already a bare `PopAsync`) — the change is purely position, label, and visual convention, not new logic.

### Decision 2: Full structural match, mirroring `PedidoListView` specifically (owner-confirmed)
Between the app's two existing "back button" placements — `PedidoListView`'s side-panel/bottom-bar labeled-button pattern, and `DetailedWeightView`'s top-header rounded-icon (`BtnVolver`) pattern — this change follows **`PedidoListView`**, not `DetailedWeightView`. Rationale: `PedidoFormView` is reached directly from `PedidoListView` (`PushAsync`) and belongs to the same feature/list-detail pair; sharing its exact chrome (text-labeled `Regresar`, same panel/bar structure, same `Boton`/`BotonPrincipal` styles) is the more consistent choice than importing a different screen family's icon convention. This was flagged as a recommendation during issue enrichment and is finalized here since it doesn't materially change scope or acceptance criteria.

### Decision 3: Side panel / bottom bar hosts the three header actions
`Guardar`, `Eliminar` (visibility unchanged — hidden while `Concluded`), and `Regresar` move into the new panel/bar, in that order, exactly as `PedidoListView`'s `Nuevo Pedido` / `Actualizar` / `Regresar` do:
- WinUI: a `Border` side panel (`RowDefinitions="*,*,*"`, `RowSpacing="15"`), `BotonPrincipal` for `Guardar`, `Boton` for `Eliminar`/`Regresar`.
- Android: a 3-column `Grid` bottom bar (`ColumnDefinitions="*,*,*"`), same three buttons, suffixed `...Android` per the existing naming convention (`BtnSaveAndroid`, `BtnDeleteAndroid`, `BtnBackAndroid`), each wired to the same `Clicked` handlers as their WinUI counterparts.

`Eliminar`'s existing `IsVisible="False"` default (shown only when editing an existing, non-concluded pedido) carries over unchanged to both variants.

### Decision 4: Content area keeps its current internal structure
Everything currently inside the `ScrollView` — title/status labels, Provider/Fecha Esperada/Notas `Border` sections, the Lines section (`CollectionView`, add-line panel) — moves into the new content column (WinUI) / content row (Android) as-is, still wrapped in the same `ScrollView`. No internal reflow of these sections; only their container changes from "the whole page" to "the content region next to/below the action panel."

### Decision 5: AutomationIds on the touched buttons only
`AutomationProperties.AutomationId` is added to `BtnSave`, `BtnDelete`, `BtnBack` (and their `...Android` counterparts) — not swept across the rest of the screen. This gives the bot harness something stable to assert on for this change without expanding into the broader `AutomationId` sweep already tracked as a separate follow-up (`automated-test-foundations` §5.1).

### Decision 6: Verification is VM-bot regression + manual owner sign-off, not a Linux build gate, and not a new login→Pedido bot scenario
No `dotnet build`/`dotnet test` step in this change's tasks targets the MAUI head — it cannot run here, and every prior MAUI change in this repo has the same caveat. A bot scenario that drives login → navigation → `PedidoFormView` is explicitly **not** built here: `BasculaBotTests` today only covers the login screen (`AppLaunchSmokeTests`), and login/navigation automation is the separately-deferred `automated-test-foundations` §5.2 follow-up — building it inline here would be scope creep beyond this UI-normalization change. Instead:
1. Decision 5's `AutomationId`s land now so that follow-up bot scenario, once built, can already target this screen's buttons.
2. The existing `AppLaunchSmokeTests` regression check must still pass unmodified on `win10-maui-dev` — it catches an app that fails to start, which the layout restructure has no reason to affect, but is cheap insurance.
3. Tasks end with an explicit, unchecked gate: publish to `win10-maui-dev`, run the existing bot suite, then **ask the project owner to run the app, open a Pedido, and visually confirm the new layout** before the change is considered done — approving it, or sending back concrete UI change requests.

This mirrors how EF migrations are handled in this repo (scaffold-only is fine without a DB connection; applying/running is the owner's job) — here, generating/reviewing the XAML is fine in this sandbox; rendering and approving it is the owner's job.

### Decision 7: Productos section rework (owner feedback, round 1)

After the §5 verification pass, the owner approved the side panel and the header fields (Provider/Fecha/Notas) as built, but flagged the Productos del Pedido section on two counts: text contrast in light theme, and a "chonky" per-line layout.

**Root cause of the contrast issue:** `App.xaml`'s shared `Style x:Key="Label"` sets a static `TextColor="{StaticResource 1}"` (`#F4F4F2`, near-white) with no `AppThemeBinding` — fine on the dark `BlackGray` card background, unreadable on the light-theme `WhiteGray` (`#80D3D3D3` over an already near-white page). This is a pre-existing bug, not something this change introduced, but the line cards were the most visible place it showed up because most of their labels never overrode it (unlike `PedidoListView`'s cards, which override `TextColor` per label). Fixed by adding explicit `AppThemeBinding` `TextColor` to every previously-uncolored label in the Productos section. `LblProviderName` has the same latent gap but the owner approved that section as-is without flagging it — left untouched rather than expanding scope unilaterally.

**Layout choice — "slim row, tap to expand"** (owner picked this over a "compact static card" alternative): each `PedidoLineViewRow` now carries a bindable `IsExpanded` (mirrors `WeightEntryDetailRow`'s existing `INotifyPropertyChanged` pattern — first precedent for that on a `Models/` class besides `WeightEntryDetailRow`) and a computed `ChevronGlyph`. The line card's `DataTemplate` splits into an always-visible collapsed summary row (chevron, product name, pending amount, a small colored status dot + text) and an `IsVisible="{Binding IsExpanded}"`-gated detail block (full amounts, the dis-tare badge, Pesar/Cerrar). Tapping the summary row toggles `IsExpanded` via a `TapGestureRecognizer` — no ViewModel/service change, purely a local view-model (row) property.

Trade-off accepted: expand state is per-row-instance, and `PedidoFormViewModel.RebuildLineRows` replaces all row instances on every reload (add-line, convert, close) — so a row collapses again after any mutation. Not preserved on purpose; not worth the complexity for a rarely-annoying reset, and every other piece of screen state already fully reloads the same way.

**Button sizing:** the global `Boton` style (`WidthRequest` up to 280 on WinUI) was the other big contributor to "chonky" — sized for whole-page actions, not inline per-item buttons. New `BotonChico` style (`App.xaml`, `BasedOn="{StaticResource Boton}"`, same colors/border behavior, ~1/3 the footprint) is now used for Pesar/Cerrar. Available for reuse anywhere else in the app that needs a compact inline button.

**Dis-tare checkbox/badge:** owner chose to leave it as-is. Confirmed while investigating (not actioned): `RequiresDisTaring` is fully wired end-to-end (checkbox → DTO → entity → DB) but never read anywhere that drives weighing-screen behavior — flagged to the owner as a possible future cleanup, out of scope here.

## Risks / Trade-offs

- Restructuring the container `Grid` risks breaking the existing `x:Name` bindings used throughout `PedidoFormView.xaml.cs` (`LblTitle`, `LblStatus`, `BtnPickProvider`, `LinesSection`, `LinesCollectionView`, etc.) if any move gets the `Grid.Row`/`Grid.Column` attachment wrong — mitigated by keeping every existing named element and only changing their containing layout, not removing/renaming any (aside from `BtnCancel` → `BtnBack` per Decision 1).
- `OnPageLoaded`'s current toggling of `BtnSave.IsVisible = false` / `BtnPickProvider.IsEnabled = false` / etc. when `Concluded` must keep working against the same named controls after the restructure — no `x:Name` should be dropped for the controls that logic already touches.
- No automated regression coverage exists for MAUI layout beyond the one new bot assertion — visual confirmation stays the owner's responsibility, which is why Decision 6's manual gate is a hard requirement of this change, not a nice-to-have.

## Open Questions

None — both scope-defining decisions were confirmed with the owner during issue enrichment (see issue #129) and finalized above.
