## Why

Issue #135 flagged that #134 (roles/permissions) only reconciled one of `EditSettingsView`'s pre-existing device-local `Preferences` toggles (`ManualWeight` → `CanCaptureWeightManually`). The rest — `SecondaryTerminal`, `OnlyPedidos`, `OnlyFinished`, `RequirePartner`, `BypasTurn` — were left as plain per-device booleans with zero relationship to who's actually logged in, even though `Dispatching Operator` was named during #134's own scoping specifically as "a second operator tier for the secondary terminal" and never got wired up that way.

Working through this issue's own open questions with the project owner reached firm conclusions instead of leaving them as further audit items:

- **`SecondaryTerminal` and `OnlyPedidos` are removed as device settings entirely.** Which mode a terminal operates in should be determined by *who is logged in*, not by a checkbox any operator could flip on any machine. `DispatchingOperator` becomes, literally, the secondary-terminal role — closing the exact gap the role was named for and never finished.
- **`OnlyPedidos` has no home in the existing 5 roles** — logging incoming pedido deliveries is a genuinely different job from weighing dispatch — so this change adds a 6th role, `PurchasingOperator`.
- **`OnlyFinished`** doesn't fit a role-default the way the other two do (no role is "just a reprint kiosk"), but the owner confirmed it should still be reachable through the same role-driven mechanism — via the per-user override, not a role default.
- **`BypasTurn`** turns out to override `WeightLogisticService`'s single-scale mutual-exclusion lock (the exact "bascula ocupada" mechanism fixed earlier this session), not a queue/ticket system as the issue's own table guessed. Given what it actually overrides, it stays a device setting but now requires the self-authorize gate at the moment of use, not just a silent standing permission.
- **`RequirePartner` is confirmed dead in production** — removed outright, along with the client-side condition that read it (the unrelated `Providers` check that also triggers requiring a partner is untouched).

## What Changes

- **`Role` gains a 6th value, `PurchasingOperator`**, appended after `Sudo` (ordinal 5) so every existing stored `Role` int is unaffected.
- **New `TerminalMode` enum** (`Main`, `Secondary`, `PedidosOnly`, `OnlyFinished`) with a role-default table (`Operator`/`Supervisor`/`Admin`/`Sudo` → `Main`, `DispatchingOperator` → `Secondary`, `PurchasingOperator` → `PedidosOnly`) and a nullable per-user `TerminalModeOverride`, resolved the same way the two existing ABAC permission flags are (role default unless overridden) — this is how `OnlyFinished` becomes reachable despite having no role default of its own.
- **The resolved `TerminalMode` ships in the login response** (`UserDto`), and the client reads it from the current session instead of four separate `Preferences` keys.
- **`EditSettingsView` loses the `SecondaryTerminal`, `OnlyPedidos`, `OnlyFinished`, and `RequirePartner` checkboxes.** `BypasTurn` stays, but tripping it at weigh-time now prompts for a `GateIdentifier`/`GatePassword` credential, verified through the existing `IGateAuthorizationService` (same mechanism as the seven other self-authorize-gated actions) before the scale-lock override is allowed.
- **`RequirePartner`'s Preference and its read site are deleted**; the pre-existing, unrelated `Providers` requirement is untouched.

## Non-goals

- Any change to `CanSelfAuthorizeGate`/`CanCaptureWeightManually` or their existing role defaults — untouched.
- The purely cosmetic/config `Preferences` (`ShowDocumentTypeFilter`, `PreferedDocumentType`, `PurchaseExternalTarget`, `FilterNull`, `HideTaskbar`, `AppTheme`, `HostUrl`) — out of scope, they have no per-user angle.
- Backfilling `TerminalModeOverride` on any existing user — nullable, defaults resolve from `Role` automatically; the owner assigns overrides (e.g. for an `OnlyFinished` kiosk account) whenever convenient.
- Re-diagnosing the underlying WinUI focus bug or the weight-lock mechanics themselves — both already handled in `fix-session-inactivity-timeout`.

## Impact

**API/Domain:** `Role` (+1 value), new `TerminalMode` enum, `User.TerminalModeOverride`, `PermissionService`-style resolution, `UserDto`/`CreateUserRequest`/`UpdateUserRequest`, a new lightweight gate-verification endpoint for `BypasTurn`, an EF Core migration (nullable column only — retro-compatible, same pattern as `InactivityTimeoutMinutes`/`Name`/`LastName`).
**Client (`BasculaInterface`):** `EditSettingsView` (UI + code-behind), every call site currently reading `SecondaryTerminal`/`OnlyPedidos`/`OnlyFinished`/`RequirePartner` from `Preferences` (`BasculaViewModel`, `DetailedWeightViewModel`, `PendingWeightsViewModel`, `WeightingScreen`, `DetailedWeightView`, `PendingWeightsView`, `MainPage`, `Models/WeightEntryDetailRow`), and the three `BypasTurn` call sites gain a gate-credential prompt.
**Existing data:** zero impact — `Role`'s new value is appended, not inserted; `TerminalModeOverride` is nullable and every existing row resolves to its role's default automatically.
