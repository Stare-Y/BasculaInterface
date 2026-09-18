# Proposal

## Why

`BasculaBotTests` (the Windows-VM FlaUI bot harness) currently has one test that only proves it can launch the app and see the login screen — it drives no real flow. Since `role-driven-terminal-modes` and `add-user-authentication-and-audit-log`, the app has six real roles with role-derived landing screens and permission gates, but nothing catches a regression in login, terminal-mode routing, the cross-terminal weight lifecycle, or the self-authorize gate. Only `PedidoFormView` currently carries `AutomationId`s, so the harness cannot yet drive any other screen.

## What Changes

- Extend `BasculaBotTests` with role-based roleplays covering three flow categories: (1) login + role-derived landing screen for all six roles, (2) the cross-terminal weight-entry lifecycle using direct product/partner pickers (not pedido-line conversion — out of scope this round), and (3) permission-gated actions (self-authorize gate, manual weight capture) exercised per role default.
- Add `AutomationProperties.AutomationId` to the previously-uninstrumented screens these roleplays drive: `MainPage`, `PendingWeightsView`, `FinishedWeights`, `WeightingScreen`, `DetailedWeightView`, `ReadOnlyDetailedWeightView`, `AuthorizeTurnBypassPopUp`, `ProductSelectView`, `PartnerSelectView`.
- Add idempotent test-user provisioning: one persistent `BOT<ROLE>` user per role, created via the real `Admin`/`Sudo` API on first run and corrected back to its template (role defaults only, no overrides) on later runs if it drifts — never deleted/recreated, never seeded via migration.
- Extend `AppDriver` with element-interaction helpers (find-by-`AutomationId`, click, type, wait) beyond its current diagnostics/screenshot-only surface.
- Update `scripts/vm/run-bot-suite.ps1` to read bootstrap-account credentials from new VM environment variables and start a real API by default (roleplays need one; the old launch-only smoke test didn't).
- Correct `scripts/vm/README.md`'s stale claim that manual-weight-capture lives in Settings — it's on `WeightingScreen` now, gated by `CanCaptureWeightManually`.

## Non-goals

- Pedido CRUD and pedido-line-to-weight conversion (`ConvertLineToWeightPopUp`, the "Pesar" button) — explicitly deferred; this batch attaches products via the direct product/partner pickers instead.
- The zero-tare "new entry from Main" distare workaround for products needing the main scale — a real but unenforced operational convention, deferred to a follow-up change.
- Any account-seeding mechanism — provisioning stays API-driven per the existing design decision (`role-permission-model`).
- Running the suite from Linux or in CI — it remains Windows-VM-only.
- Bootstrap account provisioning itself — assumed to already exist on the target DB.

## Capabilities

### New Capabilities

_None._ This extends the existing bot-harness capability rather than introducing a new one.

### Modified Capabilities

- `testing-infrastructure`: the Windows-VM bot harness requirement grows from "launch + read login screen" to "drive role-based roleplays covering login/landing, weight lifecycle, and permission gates," plus a new requirement for idempotent, template-reconciled test-user provisioning via the real API.

## Impact

- **Terminals affected**: Main, Secondary, and Customer Service (PedidosOnly) terminal modes are all driven by roleplays as login landings; `FinishedWeights` (the `OnlyFinished` screen) is exercised via Main-mode's "Ver Finalizados" navigation, not as a login landing, since this batch's templates use pure role defaults with no `TerminalModeOverride`.
- **Code**: `src/backend/BasculaBotTests/*` (new roleplay test classes, `AppDriver` extensions), MAUI XAML views listed above (`AutomationId` additions only — no behavior change), `scripts/vm/run-bot-suite.ps1` (+ README correction).
- **APIs consumed** (not modified): `POST /api/Auth/Login`, `POST /api/Users`, `PUT /api/Users/{id}`, `GET /api/Users`.
- **Related existing specs exercised but not changed**: `role-permission-model`, `terminal-mode-assignment`, `session-management`, `self-authorize-gate`, `weight-entry-partner-change`, `weight-detail-product-change`, `weight-entry-conclude`.
- **Dependencies**: none new; reuses FlaUI/UIA3 already in `BasculaBotTests.csproj`.
