## Why

The client-side inactivity logout added by `add-user-authentication-and-audit-log` (design.md Decision 7) shipped incomplete and with a real bug, surfaced by a production incident: the operator backgrounded the app for over 10 minutes, expected an automatic logout, but the screen never changed — and afterward the UI accepted hover but not clicks on a normal (non-popup) screen. Investigation of `InactivityWatcherService`, `MainPage.xaml.cs`, and every navigation call site in `BasculaInterface` found two confirmed, code-level bugs:

1. **No global activity hook exists.** `InactivityWatcherService.RegisterActivity()` is called exactly once, at login. The watcher's own doc comment says a tap/pointer hook at the `AppShell` root was the caller's responsibility — it was never added. The timer is therefore not tracking inactivity at all; it fires a fixed 10 minutes after login regardless of use.
2. **The timeout handler pops the wrong navigation stack.** `OnInactivityTimeout` calls `Shell.Current.Navigation.PopToRootAsync()`, which only unwinds MAUI's plain `NavigationStack`. Nearly every real screen in this app (`PendingWeightsView`, `WeightingScreen`, `FinishedWeights`, `EditSettingsView`, `PartnerSelectView`, `ProductSelectView`, `PedidoListView`) is opened via `PushModalAsync` onto the separate `ModalStack`, which `PopToRootAsync()` never touches. On timeout, the session token is wiped from `SecureStorage` silently while the operator's current screen is left exactly as-is — worse than no timeout at all, since nothing visibly indicates the session ended.

The exact mechanism behind the reported click-freeze itself remains unconfirmed (a WinUI-level input-routing quirk after a long-idle window is a live possibility, independent of this code), but firing an unguarded, unprompted Shell navigation call at an arbitrary moment — exactly what bug 2 does — is a plausible contributor and is hardened defensively regardless.

Separately, the project owner requested the timeout duration become configurable per role (rather than the current hardcoded 10 minutes for everyone), since a `Sudo` session left unattended is a materially different risk than an `Operator`'s.

## What Changes

- **Global activity hook**: a tap/pointer gesture recognizer at the `AppShell` root, plus resetting on every successful API response, both calling `InactivityWatcherService.RegisterActivity()` — completing the mechanism design.md Decision 7 originally called for.
- **Timeout handler pops the actual visible stack**: `OnInactivityTimeout` drains `Navigation.ModalStack` (not just `NavigationStack`) before returning to the login screen, guarded against colliding with any navigation already in flight.
- **Per-user, role-defaulted inactivity timeout**: new non-nullable `User.InactivityTimeoutMinutes` (DB default `5`, for any row inserted outside the app). `UserService` seeds it from role at creation time if not explicitly supplied: `Sudo`=2, `Admin`=5, `Supervisor`=5, `Operator`=10, `DispatchingOperator`=20. The client reads it from the logged-in user's session instead of a hardcoded constant.

## Non-goals

- Diagnosing or fixing a possible OS/WinUI-level input-routing bug after a long-idle window — that investigation is ongoing separately and is not blocked on this change.
- Restoring a saved session across a full app relaunch (`SessionService.RestoreAsync()` exists but is never called) — a separate, pre-existing gap, unrelated to this incident.
- Re-fetching or changing the timeout value mid-session; it is fixed for the lifetime of a login, same as today.
- Any change to `BasculaUi` (React dashboard) or to server-side session tracking (still explicitly out of scope per the parent change's Decision 7).

## Impact

**Affected terminals:** MAUI terminal (`BasculaInterface`) only, all terminal modes — the login/session/timeout code is app-wide, not mode-specific.
**Client:** `InactivityWatcherService`, `AppShell`, `MainPage.xaml.cs`, `AuthHeaderHandler`.
**API/Domain:** `User` entity gains `InactivityTimeoutMinutes`; `UserDto`/`LoginResponse` expose it; `UserService` creation logic; a new EF Core migration.
