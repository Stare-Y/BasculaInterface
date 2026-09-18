# Tasks

## 1. AppDriver interaction layer

- [x] 1.1 Add `FindByAutomationId(window, id)`, `Click(element)`, `TypeText(element, text)`, and `WaitForElement(window, id, timeout)` helpers to `AppDriver`; verify by writing a throwaway unit-level call against a mocked/no-op window compiles and the existing `AppLaunchSmokeTests` still passes unchanged.

## 2. AutomationId instrumentation (additive-only XAML edits, no behavior change)

- [x] 2.1 `MainPage.xaml`: add `AutomationId` to `IdentifierEntry`, `PasswordEntry`, `BtnLogIn`, `LoginErrorLabel`; verify by grepping the file for the four new attributes and confirming the app still builds for `net8.0-windows` (VM/Windows step — see task 7.1 for full verification). (Extended during 6.1-6.3 implementation: also `BtnSettings` — see the `EditSettingsView` note under 6.1.)
- [x] 2.2 `PendingWeightsView.xaml`: add `AutomationId` to `BtnNewWeighProcess`, `BtnNewWeightLessPedido`, `BtnFinished`, `PendingWeightsCollectionView`, and the per-row `Border` template (`BtnSeleccionar`) so a specific row can be selected; verify by grep + VM build.
- [x] 2.3 `FinishedWeights.xaml`: add `AutomationId` to `PendingWeightsCollectionView`, `BtnExit`; verify by grep + VM build.
- [x] 2.4 `WeightingScreen.xaml`: add `AutomationId` to `EntryVehiclePlate`, `EntryNotes`, `BtnCaptureNewWeight`, `BtnSetTaraInicial`, `CheckBoxUseManual`, `EntryLabel`, `BtnPickPartner`, `BtnPickProduct`; verify by grep + VM build.
- [x] 2.5 `DetailedWeightView.xaml`: add `AutomationId` to `BtnNuevoProducto`, `BtnNewEntry`, `BtnFinishWeight`, `CollectionViewWeightDetails`, and the per-row "mark as loaded" button; verify by grep + VM build. (Extended during 5.2 implementation: also `BtnPickPartner` and `PickerTargetBehavior` — the partner/ExternalTargetBehavior gate for `BtnNuevoProducto` lives on this screen, not `WeightingScreen`, confirmed with the project owner; `BtnNuevoProducto` isn't disabled when blocked, it shows a `DisplayAlert` on click.)
- [x] 2.6 `AuthorizeTurnBypassPopUp.xaml`: add `AutomationId` to `IdentifierEntry`, `PasswordEntry`, `btnConfirm`, `btnCancel`; verify by grep + VM build.
- [x] 2.7 `ProductSelectView.xaml`: add `AutomationId` to `SearchBar`, `ResultsCollectionView`, `LabelResultado`, and the per-row selectable item; verify by grep + VM build.
- [x] 2.8 `PartnerSelectView.xaml`: add `AutomationId` to `SearchBar`, `ResultsCollectionView`, `LabelResultado`, and the per-row selectable item; verify by grep + VM build.

## 3. Idempotent test-user provisioning

- [x] 3.1 Define the six `BOT*` templates (`BOTOPERATOR`, `BOTDISPATCH`, `BOTSUPERVISOR`, `BOTADMIN`, `BOTSUDO`, `BOTCUSTSVC`) as a fixture matching `design.md`'s table (role only, no permission/terminal-mode overrides); verify by a unit-level assertion that the fixture's six entries match the table exactly.
- [x] 3.2 Implement the provisioning routine: log in as bootstrap Admin/Sudo, `GET /api/Users`, create missing templates via `POST`, correct drifted ones via `PUT`, skip matching ones; verify against a live API (VM) that a fresh DB ends up with all six users, and a second consecutive run makes zero `POST`/`PUT` calls (per the spec's "matching user triggers no mutation" scenario).
- [x] 3.3 Read `BasculaBotAdminIdentifier`, `BasculaBotAdminPassword`, `BasculaBotRolePassword` from environment variables in the provisioning routine; fail fast with a clear message if the two bootstrap variables are unset; verify by running the suite locally with them unset and confirming it fails before any HTTP call, then with them set and confirming provisioning proceeds.
- [x] 3.4 Update `scripts/vm/run-bot-suite.ps1` to pass these environment variables through, and make starting a live API the default (roleplays need one) rather than the `-StartApi` opt-in; verify by running the script on the VM and confirming the API starts automatically and the suite can log in.
- [x] 3.5 Correct `scripts/vm/README.md`'s stale claim that manual weight capture lives in Settings → "Manual" (it's `WeightingScreen`'s `CheckBoxUseManual` now, gated by `CanCaptureWeightManually`); verify by re-reading the corrected paragraph against `WeightingScreen.xaml`.

## 4. Login + role-based landing roleplays

- [x] 4.1 Roleplay: `Operator`/`Supervisor`/`Admin`/`Sudo` log in and land on `PendingWeightsView` with `BtnNewWeighProcess` and `BtnFinished` visible/enabled; verify by a passing FlaUI test asserting those elements' `IsEnabled`/visibility for each of the four roles.
- [x] 4.2 Roleplay: `Dispatching Operator` logs in and lands on `PendingWeightsView` with `BtnNewWeighProcess`/`BtnFinished` hidden; verify by asserting those elements are absent or not visible.
- [x] 4.3 Roleplay: `Customer Service` logs in and lands on `PendingWeightsView` with `BtnNewWeightLessPedido` visible and `BtnNewWeighProcess` hidden; verify by asserting the swapped visibility.
- [x] 4.4 Roleplay: from a Main-mode session, tap `BtnFinished` and assert navigation to `FinishedWeights` with its collection view populated; verify by the FlaUI test passing.

## 5. Weight-entry lifecycle roleplays (direct pick, no pedido)

- [x] 5.1 Roleplay: Main-mode session captures `EntryVehiclePlate` + `EntryNotes` (driver) and confirms the empty-truck weight via `BtnCaptureNewWeight`; verify by asserting a new pending entry appears in `PendingWeightsView` with the captured plate.
- [x] 5.2 Roleplay: from that entry, assign the fixed test partner ("Claro Cervantes", the sole `PartnerSelectView` search result) and select any `ExternalTargetBehavior` option, then confirm the product picker only becomes usable after both are set. **Corrected during implementation** (confirmed with the project owner): this gate is `DetailedWeightView.BtnNuevoProducto`, not `WeightingScreen.BtnPickProduct` — `WeightingScreen` has no `ExternalTargetBehavior` picker at all. `BtnNuevoProducto` isn't disabled when blocked; it shows a `DisplayAlert` on click. `BtnPickPartner`/`PickerTargetBehavior` on `DetailedWeightView` were given `AutomationId`s to support this (see 2.5's amendment). Verified by asserting the click is blocked (alert) before, opens `ProductSelectView` after.
- [x] 5.3 Roleplay: pick a product via `ProductSelectView` and attach it directly to the entry (no pedido line); verify by asserting the entry now has at least one `WeightDetail`.
- [x] 5.4 Roleplay: `Dispatching Operator` session opens that product detail, captures `BtnSetTaraInicial` first (since `SecondaryTare` is unset), then captures the full weight and marks it loaded; verify by asserting the detail's `IsLoaded` becomes true and the tare button is gone afterward.
- [x] 5.5 Roleplay: Main-mode session opens `DetailedWeightView` for the now-fully-loaded entry and taps `BtnFinishWeight`; verify by asserting the entry's conclude state (e.g. it disappears from `PendingWeightsView` and/or appears in `FinishedWeights`).
- [x] 5.6 Decide and document (in a code comment, not a spec change) whether 5.1–5.5 run as one continuous session or as separate per-role logins matching the real Main→Customer-Service-style handoff (`design.md` Open Question — pick the simpler single-session approach first; verify by the full sequence 5.1–5.5 passing end to end).

## 6. Permission-gated action roleplays

- [x] 6.1 Roleplay: `Supervisor` (and separately `Sudo`) self-authorizes the gate via `AuthorizeTurnBypassPopUp` using their own credentials and `btnConfirm` succeeds; verify by asserting the popup closes without an error state. **Precondition discovered during implementation** (confirmed with the project owner): the popup only ever appears when the device-local `BypasTurn` preference is on (toggled only via `EditSettingsView.CheckBoxBypasTurn` — no other in-app path exists) AND the scale is already busy (`CanWeight()` false). Added `AutomationId`s to `MainPage.BtnSettings`, `EditSettingsView.CheckBoxBypasTurn`, `EditSettingsView.BtnCancel` (as `EditSettings_BtnCancel`) to reach it, and the roleplay opens a second app instance to genuinely hold the weight lock (both instances share the same deviceId on the VM) rather than faking the busy state. Since the popup itself always closes on confirm regardless of outcome, "succeeds"/"is rejected" (6.1-6.3) are verified by whether `WeightingScreen` opens afterward vs. an "Error"/"Bascula ocupada" alert appears.
- [x] 6.2 Roleplay: `Operator` (no override) attempts the same gate with their own credentials and it is rejected; verify by asserting an error/rejection state on the popup.
- [x] 6.3 Roleplay: `Customer Service` (no override) attempts the same gate and it is rejected; verify by the equivalent assertion.
- [x] 6.4 Roleplay: `Supervisor`/`Sudo` sees `CheckBoxUseManual` available on `WeightingScreen` and can type a manual weight; verify by asserting the checkbox is present/enabled and a typed value reaches `EntryLabel`'s bound value.
- [x] 6.5 Roleplay: `Operator` (no override) does not have manual capture available; verify by asserting the checkbox is hidden or disabled. **Scope note**: `Customer Service` dropped from this check — their `PedidosOnly` flow never opens `WeightingScreen` at all (products go through `PickQuantityPopUp` instead), so `CheckBoxUseManual` is structurally unreachable for that role rather than hidden by a permission check; asserting its absence there would test navigation, not the permission gate.

## 7. Verification

- [ ] 7.1 Run the full suite on `win10-maui-dev` (or another Windows PC) via `scripts\vm\run-bot-suite.cmd` with all required environment variables set; verify by a green `.trx` covering all roleplays in groups 4–6 and no leftover failures in `artifacts/bot-suite/`.
- [ ] 7.2 Confirm a second consecutive run of 7.1 performs zero user-provisioning mutations (task 3.2's idempotency scenario) by inspecting request logs or an added debug assertion; verify by the count of `POST`/`PUT /api/Users` calls being zero on the second run.
