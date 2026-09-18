# Proposal

## Why

Issue #142: the same user account sometimes needs to work at terminals meant for different workflows. Multiple accounts per person was already discarded. The remaining candidate — forcing behavior via the user's own profile/override — has a silent gap: a user with no override configured for a given terminal falls back to their own role's default `TerminalMode`, so e.g. an Admin at a terminal meant for the `Dispatching Operator` workflow renders Admin's own UI and can skip a required step (destarar) before weighing, corrupting the weight result. This is a domain-correctness problem in the action sequence a terminal walks an operator through, not a permissions problem — so it needs to be fixed at the terminal, not the user.

## What Changes

- Introduce a terminal-identity concept (does not exist today — the client's `DeviceName` currently has no auth-side meaning).
- A terminal can be configured with a **forced role**, which pins its resolved `TerminalMode` regardless of which user logs in there.
- Introduce a role rank, a new ordering separate from the `Role` enum's stored ordinal values (which stay frozen): `Dispatching Operator` (lowest) → `Customer Service` → `Operator` → `Supervisor` → `Admin` → `Sudo` (highest).
- Login is denied when the authenticating user's rank is below the terminal's forced role's rank.
- A configured terminal's forced role determines only `TerminalMode`. `CanSelfAuthorizeGate` and `CanCaptureWeightManually` are unaffected and keep resolving from the actual logged-in user's role/override.
- A terminal with no forced role configured is unaffected: `TerminalMode` continues to be computed from the logging-in user exactly as today. This is an opt-in, per-terminal rollout — configuring which terminals get a forced role is an ongoing admin responsibility, not a one-time migration this change performs.
- **BREAKING**: `POST /api/Auth/Login` gains a required terminal-identifier field, and can now reject a login for a qualified-credentials user solely due to terminal/role mismatch.

## Capabilities

### New Capabilities
- `terminal-role-enforcement`: terminal entities, each optionally carrying a forced role; the role-rank ordering; the login-time rank gate (allow/deny) against a terminal's forced role.

### Modified Capabilities
- `user-authentication`: `POST /api/Auth/Login` accepts a terminal identifier and can reject an otherwise-valid login when the user's rank is below the terminal's forced role's rank.
- `terminal-mode-assignment`: resolution order gains a new top precedence tier — a terminal's forced role (when configured) determines `TerminalMode`, ahead of the user's own role default or `TerminalModeOverride`.

## Impact

- **Backend**: new `Terminal` entity/table + migration; `AuthService.LoginAsync` (terminal lookup, rank gate, `TerminalMode` resolution); `PermissionService.GetEffectiveTerminalMode` (new precedence tier); new role-rank mapping (additive, does not touch `Role.cs` ordinals); likely a new `TerminalsController` (Admin/Sudo-only, mirroring `user-administration`'s access rule) for configuring a terminal's forced role.
- **MAUI client (`BasculaInterface`)**: login flow must send a terminal identifier — candidate: reuse existing `DeviceName` (`Preferences.Get("DeviceName", DeviceInfo.Name)`, currently only used for the scale device-lock), unconfirmed; must handle a new login-rejection reason (rank-denied) distinctly from bad credentials.
- **All terminal types are potentially affected**: Main, Secondary, PedidosOnly, and OnlyFinished terminals can each be configured with a forced role; none is exempt from this mechanism.
- **Admin portal (`BasculaUi`)**: candidate home for the terminal-configuration UI (currently only a dashboard prototype exists; not yet confirmed as the target surface — see design.md open questions).
- No change to the `Role` enum, `CanSelfAuthorizeGate`/`CanCaptureWeightManually` resolution, or account/profile model (multi-account remains out of scope).

## Non-Goals

- Multiple accounts or profiles per physical user (explicitly discarded).
- Any change to `CanSelfAuthorizeGate`/`CanCaptureWeightManually` resolution or the `Role` enum's stored ordinal values.
- Automatic backfill/migration of forced roles onto existing terminals.
- Defining exact login-denial UX/error copy (left to design.md).
