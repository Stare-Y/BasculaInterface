# Design: Role-Driven Terminal Modes

## Context

Issue #135 was deliberately scoped as an audit, not a ready-to-implement spec — its own text says several of these calls are "genuine business/policy calls" needing the owner's input, "not an assumption." Rather than run `enrich-issue-for-openspec` as a separate pass, this change's clarifying questions were asked directly during `/opsx:propose` and iterated twice as the mapping got more precise (starting from "should X be role-gated?" through to the concrete role-by-role comparison below). The conclusions below are the result of that conversation, not inference from the issue text alone.

## Goals / Non-Goals

**Goals:** eliminate `SecondaryTerminal`/`OnlyPedidos` as device-local settings in favor of role-derived behavior; give `OnlyFinished` a path into the same mechanism without forcing a role-default it doesn't have; require the self-authorize gate for `BypasTurn`'s actual scale-lock override; delete dead `RequirePartner` code.

**Non-Goals:** touching the two existing ABAC flags; the purely cosmetic/config `Preferences`; backfilling overrides on existing users; re-investigating the WinUI focus bug or the weight-lock mechanics (both closed in `fix-session-inactivity-timeout`).

## Decisions

### Decision 1: `TerminalMode` is a new enum, resolved like the ABAC flags — not folded directly into `Role`

**Chosen:** a new `TerminalMode { Main, Secondary, PedidosOnly, OnlyFinished }` enum. Its *effective* value for a user is resolved via a role-default table plus a nullable per-user override — the same shape `PermissionService.HasPermission` already uses for `CanSelfAuthorizeGate`/`CanCaptureWeightManually`:

```csharp
private static readonly Dictionary<Role, TerminalMode> RoleTerminalModeDefaults = new()
{
    [Role.Operator] = TerminalMode.Main,
    [Role.DispatchingOperator] = TerminalMode.Secondary,
    [Role.Supervisor] = TerminalMode.Main,
    [Role.Admin] = TerminalMode.Main,
    [Role.Sudo] = TerminalMode.Main,
    [Role.PurchasingOperator] = TerminalMode.PedidosOnly,
};

public TerminalMode GetEffectiveTerminalMode(User user) =>
    user.TerminalModeOverride ?? RoleTerminalModeDefaults.GetValueOrDefault(user.Role, TerminalMode.Main);
```

Unlike `HasPermission`, **`Sudo` is not special-cased here** — bypassing every *authorization* check unconditionally (design.md Decision 4 of the parent change) is a different concept from "which weighing screen behavior does this terminal show." A `Sudo` user gets the plain `Main` role default like `Operator`/`Supervisor`/`Admin`, and can still be overridden to any mode like anyone else.

**Why this reconciles two things that sound contradictory:** the owner's answers were "reuse the `Role` enum directly" (for the *default*) and "tri-state override like the ABAC flags" (for exceptions) in the same conversation. Those aren't actually in tension once separated: `Role` genuinely drives the *default* mode for `Operator`/`DispatchingOperator`/`PurchasingOperator` (three roles now double as their terminal persona, by design), but `OnlyFinished` has no such persona — nobody's job title is "reprint kiosk" — so it's only reachable through the override, exactly like a `Supervisor` can have `CanSelfAuthorizeGate` individually revoked despite the role default. This is stated explicitly here because it isn't obvious from either answer alone.

**Rejected — literally overload `Role` itself (e.g. add `OnlyFinishedKiosk` as a 7th role):** would conflate authorization tier with workstation UI behavior for a mode that, per the owner's own observation, describes a physical workstation's purpose rather than a person's job — adding a "role" that isn't really a job title breaks the pattern the other 6 roles establish.

### Decision 2: `PurchasingOperator` — a 6th role, appended (not inserted)

**Chosen:** `Role` gains `PurchasingOperator` as its 6th, last member. `Role` is stored as a plain int (no `HasConversion<string>` in `WeightDBContext`), so **the new value must be appended after `Sudo`, never inserted alphabetically or logically near `DispatchingOperator`** — inserting it earlier in the enum would silently shift every later member's underlying int and corrupt every existing stored `Role` value. This is called out explicitly because grouping it next to `DispatchingOperator` in the enum declaration would read more naturally and is the likely mistake to make without this note.

`PermissionService.RoleDefaults` (the two ABAC flags) also needs an entry for `PurchasingOperator` — defaulting to `[]` (both flags `false`), same as `Operator`/`DispatchingOperator`/`Admin`. `UserService.InactivityTimeoutDefaults` likewise needs an entry; absent one it already falls back to `5` via the existing `TryGetValue` guard (`fix-session-inactivity-timeout` design.md Decision 3), but an explicit entry is clearer than relying on the fallback. A reasonable default is `10` minutes, matching `Operator` — no stated reason for `PurchasingOperator` to differ.

### Decision 3: `BypasTurn` stays a device setting; using it now requires the self-authorize gate

**Chosen:** investigating `BypasTurn`'s actual call sites found it doesn't touch `ITurnService` (a plain GET that assigns a display ticket number, never a blocking check) despite issue #135's table describing it as "the turn/queue check" — it actually guards `BasculaViewModel.CanWeight()`, which calls `PUT /api/Weight/CanWeight`, backed by `WeightLogisticService.RequestWeight` — the single-scale mutual-exclusion lock this session already fixed a real race in (`fix-session-inactivity-timeout` §7). `BypasTurn` genuinely means "let me weigh even though another terminal currently holds the scale lock." Given what it actually overrides, per-use gating (the owner's chosen option) is implemented by reusing `IGateAuthorizationService.TryAuthorizeAsync` (identical resolution/permission check as every other gated action) behind a new, side-effect-free endpoint:

```
POST /api/Weight/AuthorizeTurnBypass   { GateIdentifier, GatePassword }  →  200 (bool)
```

The client keeps the `BypasTurn` device `Preferences` flag (a terminal still needs to be configured as "allowed to attempt a bypass" at all), but when `CanWeight()` returns `false` and `BypasTurn` is on, the client now prompts for a gate credential and calls this endpoint before proceeding — mirroring the credential popup already used for the other seven gated actions — instead of silently proceeding.

**Rejected — fold the check into the existing `RequestWeight`/`CanWeight` endpoint itself:** would require every caller (including normal, non-bypass calls) to pass gate fields it doesn't need; a separate verify-only endpoint keeps the common path unchanged and the gated path opt-in.

### Decision 4: `RequirePartner` is deleted, not migrated to anything

**Chosen:** confirmed dead in production. `Preferences.Get("RequirePartner", false)` is removed from `BasculaViewModel.ValidateBeforePosting`'s condition, leaving `if (Providers) { ... }` — the unrelated, pre-existing rule that provider deliveries always require a partner regardless of any setting. The `EditSettingsView` checkbox, its `Preferences` load/save calls, and its slot in the `OnlyFinished` mutual-exclusion group are removed entirely.

## Client-side inventory (what actually needs to change)

Every current read of the four removed `Preferences` keys, found by inspection — this is the concrete footprint `tasks.md` works through:

| Preference | Call sites |
|---|---|
| `SecondaryTerminal` | `Models/WeightEntryDetailRow.cs` (×2), `ViewModels/BasculaViewModel.cs`, `ViewModels/DetailedWeightViewModel.cs`, `Views/WeightingScreen.xaml.cs` (×4 — one more than first spotted, in `OnAppearing`'s `EntryVehiclePlate.IsEnabled`), `Views/DetailedWeightView.xaml.cs` (×3), `Views/PendingWeightsView.xaml.cs` |
| `OnlyPedidos` | `Views/DetailedWeightView.xaml.cs` (×4), `Views/PendingWeightsView.xaml.cs` (×2) |
| `OnlyFinished` | `MainPage.xaml.cs`, `Models/WeightEntryDetailRow.cs` |
| `RequirePartner` | `ViewModels/BasculaViewModel.cs` (only read site) |
| `BypasTurn` (gated per-use, not removed) | `Views/DetailedWeightView.xaml.cs` (×3 — two `CanWeight()` gates plus a distinct "row claimed by another device" `WeightedBy` check found on closer inspection of the same file), `Views/PendingWeightsView.xaml.cs` (×1 `CanWeight()` gate). `Views/WeightingScreen.xaml.cs`'s own `BypasTurn` read (skips the keep-alive renewal loop, not a check bypass) is deliberately left untouched. |

Each `TerminalMode`-derived check becomes `_sessionService?.CurrentUser?.TerminalMode == TerminalMode.X` instead of `Preferences.Get(...)`. In practice, resolution used the codebase's own already-established static-resolution pattern (`MauiProgram.ServiceProvider.GetService(typeof(ISessionService))`, already used by `MainPage`/`WeightingScreen`) rather than constructor injection — several touched classes (`BasculaViewModel`, `DetailedWeightViewModel`, `DetailedWeightView`, `PendingWeightsView`) carry a second parameterless constructor for design-time/no-DI-arg construction, which constructor injection would complicate for no benefit over the pattern this codebase already uses for exactly this service.

## Open Questions

None outstanding for the decisions above — all were resolved directly with the project owner across this conversation, including two follow-up rounds once the initial role-mapping revealed gaps (`OnlyPedidos` fitting no existing role; `OnlyFinished` not fitting the "role = job title" pattern the other mappings follow).
