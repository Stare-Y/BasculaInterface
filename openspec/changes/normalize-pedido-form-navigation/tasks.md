## 1. XAML restructure (`PedidoFormView.xaml`)

- [x] 1.1 Wrap the page in the same `OnPlatform`-driven `Grid` shape as `PedidoListView.xaml`: WinUI side-panel column + content column (`ColumnDefinitions="{OnPlatform WinUI='*,3*'}"`), Android single column with a bottom-bar row (`RowDefinitions="{OnPlatform Android='...,*,...,75'}"`).
- [x] 1.2 Add the WinUI side panel (`Border`, `RoundRectangle 30`, `DefaultShadow`, `IsVisible="{OnPlatform Android=False, WinUI=True}"`) containing, top to bottom: `BtnSave` (`BotonPrincipal`, "Guardar"), `BtnDelete` (`Boton`, "Eliminar", `IsVisible="False"` default unchanged), `BtnBack` (`Boton`, "Regresar").
- [x] 1.3 Add the Android bottom bar (`Grid ColumnDefinitions="*,*,*"`, `IsVisible="{OnPlatform Android=True, WinUI=False}"`) with `BtnSaveAndroid` / `BtnDeleteAndroid` / `BtnBackAndroid`, same texts/styles as 1.2.
- [x] 1.4 Move the existing content (`LblTitle`, `LblStatus`, Provider/Fecha Esperada/Notas `Border` sections, `LinesSection`) into the content column/row, still inside the same `ScrollView` — no change to the sections' internal XAML.
- [x] 1.5 Remove the old bottom-row `Grid` (`BtnSave`/`BtnDelete`/`BtnCancel` as one 3-column row) and the standalone `BtnCancel` entirely.
- [x] 1.6 Add `AutomationProperties.AutomationId` to `BtnSave`, `BtnDelete`, `BtnBack` and their `...Android` counterparts (Decision 5) — e.g. `PedidoForm_Save`, `PedidoForm_Delete`, `PedidoForm_Back`.

## 2. Code-behind (`PedidoFormView.xaml.cs`)

- [x] 2.1 Rename `BtnCancel_Clicked` → `BtnBack_Clicked`, wire it to both `BtnBack.Clicked` and `BtnBackAndroid.Clicked`; body stays `await Shell.Current.Navigation.PopAsync();` (Decision 1 — no behavior change).
- [x] 2.2 Wire `BtnSaveAndroid.Clicked` / `BtnDeleteAndroid.Clicked` to the existing `BtnSave_Clicked` / `BtnDelete_Clicked` handlers (same pattern `PedidoListView.xaml.cs` uses for its `...Android` buttons) — done directly in XAML's `Clicked=` attributes, no extra code-behind needed.
- [x] 2.3 Update `OnPageLoaded`'s `Concluded`-state toggling (`BtnDelete.IsVisible`, `BtnSave.IsVisible`, provider/date/notes/add-line `IsEnabled`) to also set the `...Android` counterparts, so the read-only state holds on both platform variants. (unplanned, found during implementation) `BtnSave_Clicked`'s post-first-save reveal block also set `BtnDelete.IsVisible = true` only — added `BtnDeleteAndroid.IsVisible = true` there too, same parity fix.
- [x] 2.4 Confirm no other code-behind reference to `BtnCancel` remains (`grep -rn "BtnCancel" src/backend/BasculaInterface`) — only unrelated hits in `EditSettingsView.xaml(.cs)`, out of scope.

## 3. Bot harness hooks (`src/backend/BasculaBotTests`)

- [x] 3.1 No new bot scenario is added in this change — driving login → `PedidoListView` → `PedidoFormView` depends on the separately-deferred `automated-test-foundations` §5.2 follow-up. This task exists to record that decision (Decision 6) rather than to add code.
- [ ] 3.2 Confirm `AppLaunchSmokeTests.App_starts_and_FlaUI_can_read_its_window` still passes unmodified after the restructure (regression check that the app still starts and the login screen still reads correctly) — run as part of §5 below.

## 4. Repo hygiene

- [x] 4.1 `grep -rn "ProviderPurchase\|BtnCancel"` in `PedidoFormView.xaml(.cs)` to confirm nothing stale is left behind by the restructure — clean (the one `ProviderPurchase` hit is `PedidoFormViewModel`'s historical doc-comment, unrelated to layout).
- [x] 4.2 Update any doc/comment inside `PedidoFormView.xaml.cs`'s class summary if it references the old single-column layout (check `PedidoFormViewModel`'s doc-comment too — it doesn't describe the view's layout, likely no change needed, confirm) — confirmed, no layout-describing comments existed; nothing to update.

## 5. Verification gate (owner-run — do not archive without this)

- [ ] 5.1 Publish `BasculaInterface` unpackaged on `win10-maui-dev` (`scripts/vm/run-bot-suite.ps1`, same publish step `automated-test-foundations` uses).
- [ ] 5.2 Run the existing bot suite (`scripts\vm\run-bot-suite.cmd`) — `AppLaunchSmokeTests` must stay green.
- [ ] 5.3 **Ask the project owner to run the app, open the Pedido list, create or open a pedido, and visually confirm**: the back button is present and positioned per convention, `Cancelar` is gone, `Guardar`/`Eliminar` still work, the `Concluded` read-only state still renders correctly, and the screen "feels" consistent with `PedidoListView`/other screens (the original issue's complaint).
- [ ] 5.4 On owner approval, mark this task done and proceed to `openspec-archive-change`. On a change request, capture the specific UI feedback, address it, and repeat 5.1-5.3 — do not archive on a partial/conditional approval.

## 6. Owner feedback round 1 (side panel/header approved; Productos section reworked)

Ran on `win10-maui-dev` per §5. Owner approved the side panel and the header fields (Provider/Fecha/Notas) as-is. Two issues raised against the Productos del Pedido section specifically — addressed below, per design.md Decision 7. Loops back to §5 for a fresh verification pass.

- [x] 6.1 **Contrast bug (root cause, not a design choice):** the shared `Style x:Key="Label"` (`App.xaml`) hardcodes `TextColor="{StaticResource 1}"` (a static near-white, not `AppThemeBinding`) — invisible against the line-card `WhiteGray` background in light theme. Added explicit theme-aware `TextColor` to every previously-uncolored label in the Productos section (line-card product name/pending/amounts, the add-line panel's title/product-name/dis-tare-checkbox labels, and the `EmptyView` label) — same per-label override convention `PedidoListView`'s cards already use. **Note:** `LblProviderName` in the Provider section has this same latent gap but the owner didn't flag it and approved that section as-is — left untouched; flagged for the owner to decide separately.
- [x] 6.2 **Line-card layout ("chonky"):** picked "slim row, tap to expand" over the alternative "compact static card" (owner's choice). `PedidoLineViewRow` (`Models/PedidoLineViewRow.cs`) gains `IsExpanded` (bindable, `INotifyPropertyChanged`, mirrors `WeightEntryDetailRow`'s existing pattern) and a computed `ChevronGlyph`.
- [x] 6.3 Line-card `DataTemplate` rebuilt: collapsed row (chevron, product name, pending amount, small colored status dot + text) is always visible and tappable (`LineRowHeader_Tapped` toggles `IsExpanded`); full amounts, the "Requiere destare" badge, and the Pesar/Cerrar actions move into a `VerticalStackLayout` gated on `IsVisible="{Binding IsExpanded}"`.
- [x] 6.4 New `BotonChico` style (`App.xaml`, `BasedOn="{StaticResource Boton}"`) — compact sizing for inline per-item actions; replaces the page-sized `Boton` style on the Pesar/Cerrar buttons, which was the other major contributor to the "chonky" feel.
- [x] 6.5 Dis-tare checkbox/badge: owner chose **"leave it as-is"** — no change, still fully wired, contrast-fixed alongside everything else in 6.1. (Confirmed while investigating: it's stored end-to-end but doesn't drive any weighing-screen behavior — flagged to the owner as a possible future cleanup, not actioned here.)
- [ ] 6.6 Repeat §5 on `win10-maui-dev` with these changes.
