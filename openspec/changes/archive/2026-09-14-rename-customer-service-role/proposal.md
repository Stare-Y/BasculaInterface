## Why

Investigating why a freshly-created `PurchasingOperator` user still showed `Operator`-style UI (`BtnNewWeightLessPedido` missing in `PendingWeightsView`) surfaced two separate, real issues:

- **A login regression.** `AuthService.ToDto` (backs `POST /api/Auth/Login`) never sets `UserDto.TerminalMode` — it silently falls back to the enum's default, `TerminalMode.Main`, for every user regardless of actual role or override. `UserService.ToDto` (the admin `/api/Users` endpoints) already does this correctly (`TerminalMode = _permissionService.GetEffectiveTerminalMode(user)`), so the gap is isolated to the login path. It's been live since `role-driven-terminal-modes` shipped (`0074744`) — `Secondary`/`PedidosOnly` defaults have never actually reached a client through login, for any role.
- **A naming mismatch.** `PurchasingOperator` was never really about purchasing — it's the solo-pedidos, customer-facing data-entry terminal persona. The role that actually does purchase/full-flow work (creating and managing pedidos as part of the normal weighing flow) is `Operator` and above (`Operator`, `Supervisor`, `Admin`, `Sudo`) — which already default to `Main`, unrestricted. Renaming the role to `CustomerService` makes the name match the job instead of the opposite.

Separately, `role-driven-terminal-modes`'s code shipped but the change was never archived, so `openspec/specs/role-permission-model/spec.md` (still "five fixed roles") and `openspec/specs/user-authentication/spec.md` (login-response spec doesn't mention `TerminalMode` at all) are stale.

## What Changes

- Fix `AuthService.ToDto` to set `TerminalMode = _permissionService.GetEffectiveTerminalMode(user)`, matching `UserService.ToDto`.
- Rename `Role.PurchasingOperator` → `Role.CustomerService` — same ordinal/stored int (Role is a plain `integer` column, no string conversion), so this is a zero-migration source rename across `Role.cs`, `TerminalMode.cs` doc comments, `PermissionService.cs` (both `RoleDefaults` and `RoleTerminalModeDefaults`), and `UserService.cs` (`InactivityTimeoutDefaults`). Behavior is identical to today's `PurchasingOperator`: default `TerminalMode.PedidosOnly`, both ABAC flags `false`, 10-minute inactivity default.
- Reaffirm, without code change: `DispatchingOperator` stays `Secondary` by default; `TerminalModeOverride` remains the exception mechanism for one-off terminals, not the primary way to configure the fleet — role assignment is.
- Sync the stale specs: `role-permission-model` (six roles, correct names), `user-authentication` (login response now documents `TerminalMode`), and add the never-synced `terminal-mode-assignment` capability to main specs (previously only in the still-unarchived `role-driven-terminal-modes` change), written directly with the `CustomerService` name.

## Non-goals

- No new permission/ABAC flag for "purchasing movements" — none exists today and none is needed; `Operator` and above already get unrestricted `Main` mode, which is all that sentence describes.
- No change to `DispatchingOperator`/`Secondary`.
- No change to `BypasTurn`/`AuthorizeTurnBypass` gate mechanics, and no fix to `self-authorize-gate/spec.md`'s own (separate, pre-existing) staleness re: that endpoint — flagged, not addressed here.
- No DB migration — `Role` stays a plain int at the same ordinal.
- Archiving the `role-driven-terminal-modes` change folder itself is not performed here (this change's spec deltas supersede its role/terminal-mode content); left as a follow-up cleanup task.

## Impact

**Terminals affected:** every terminal, at login — the bug fix corrects `TerminalMode` resolution for *all* roles (`Operator`/`Supervisor`/`Admin`/`Sudo`/`DispatchingOperator`/`CustomerService`), not just the renamed one. Any account currently relying on the buggy `Main` fallback will see its real mode take effect once this ships.

**API/Domain:** `Role` (rename, no new member), `AuthService.cs` (bug fix), `PermissionService.cs`, `UserService.cs`.

**Client (`BasculaInterface`):** no code change required — `CurrentTerminalMode` consumers read the resolved enum value from the session, which resolves identically post-rename; they'll simply start receiving the correct value from login instead of always `Main`.

**Existing data:** zero impact — same stored ordinal, same resolved behavior for anyone already assigned this role.
