# Design: Fix Session Inactivity Timeout & Per-Role Configuration

## Context

`add-user-authentication-and-audit-log` (design.md Decision 7) introduced `InactivityWatcherService`, a `System.Timers.Timer`-based countdown meant to log the operator out client-side after a period of inactivity. Its own task list (7.4) flagged it as "not fully wired": only `MainPage`'s login calls `RegisterActivity()`/`Start(...)`; no global activity hook was ever added. This change was never build-verified on the MAUI target in the sandbox that authored it (task 7.7), and a real-machine incident has now surfaced the consequences: left backgrounded past the timeout, the app never visibly logged out, and the operator subsequently found the UI accepting hover but not clicks.

Reading every navigation call site in `BasculaInterface` found the root of the visible half of the bug directly: this app opens essentially every screen via `Shell.Current.Navigation.PushModalAsync` (`PendingWeightsView`, `WeightingScreen`, `FinishedWeights`, `EditSettingsView`, `PartnerSelectView`, `ProductSelectView`, `PedidoListView`), landing them on MAUI's `ModalStack`. `OnInactivityTimeout`'s `Shell.Current.Navigation.PopToRootAsync()` only unwinds the separate `NavigationStack` — it cannot reach any of those screens.

The click-freeze's exact mechanism is not confirmed. `WindowsPackageType` is `None` (unpackaged), which rules out Windows Process Lifecycle Management suspend/resume as a cause — an unpackaged process keeps running normally while minimized, so its timers and threads are never frozen by the OS. A plain WinUI input-routing quirk on a long-idle window remains possible independent of any code here; that stays open, separate from this change (see Non-goals). This change fixes the two confirmed bugs and hardens the timeout handler defensively against the leading code-level hypothesis (an unguarded, unprompted navigation call firing at an arbitrary moment).

## Goals / Non-Goals

**Goals:**
- Make the inactivity timer actually track inactivity (global activity hook), not just time-since-login.
- Make the timeout's logout reach whatever screen is actually visible, regardless of which navigation stack it's on.
- Prevent the timeout's own navigation call from executing concurrently with an in-flight, user-triggered one.
- Make the timeout duration a per-role-defaulted, per-user, server-provided value instead of a hardcoded client constant.

**Non-Goals:** diagnosing a possible OS/WinUI input-routing bug on long-idle windows; wiring `SessionService.RestoreAsync()` (dead code, pre-existing, unrelated); re-fetching the timeout mid-session; any server-side session/last-activity tracking (still out of scope per the parent change).

## Decisions

### Decision 1: Global activity hook via `AppShell` root gesture + successful API responses

**Chosen:** Add a `TapGestureRecognizer` (covering pointer/touch press) to the root layout in `AppShell.xaml`, calling `InactivityWatcherService.RegisterActivity()` on every recognized tap without consuming or altering existing input handling (`Cancelled`/normal bubbling unaffected — MAUI gesture recognizers observe rather than intercept unless a child explicitly handles the same gesture). Also call `RegisterActivity()` from `AuthHeaderHandler.SendAsync` after any response with `IsSuccessStatusCode == true`, so background API activity (e.g. a scale-polling call) counts as activity too, matching the original design.md Decision 7 intent ("reset also on every successful API response").

**Rejected — per-page gesture recognizers:** would require touching every `ContentPage` in the app individually and would miss any page added later; a single root-level hook covers the whole app by construction.

### Decision 2: `OnInactivityTimeout` drains the modal stack, guarded against concurrent navigation

**Chosen:**
```csharp
private readonly SemaphoreSlim _navigationGate = new(1, 1);

private void OnInactivityTimeout()
{
    MainThread.BeginInvokeOnMainThread(async () =>
    {
        if (!await _navigationGate.WaitAsync(0))
            return; // a user-triggered navigation is already in flight; don't collide with it

        try
        {
            await _sessionService.LogoutAsync();
            _inactivityWatcher.Stop();

            INavigation nav = Shell.Current.Navigation;
            while (nav.ModalStack.Count > 0)
                await nav.PopModalAsync(animated: false);

            await nav.PopToRootAsync(animated: false);
        }
        finally
        {
            _navigationGate.Release();
        }
    });
}
```
`WaitAsync(0)` (zero timeout) means: if some other navigation already holds the gate, the timeout simply skips this cycle rather than queuing up and firing later mid-way through whatever the user is doing — safe to skip because the watcher was clearly just reset by the very activity holding that gate. Draining `ModalStack` first (last-pushed-first, matching how `PushModalAsync` stacks) then calling `PopToRootAsync()` handles both modal-opened screens and any regular-stack pages pushed inside a modal (e.g. `PedidoFormView`, `DetailedWeightView` via `PushAsync`). `animated: false` avoids stacking multiple pop transitions when several modals are queued.

**On the gate's scope:** this only guards this app's *own* navigation call sites from colliding with the timeout's; it does not require auditing every existing `PushModalAsync`/`PopAsync` call site elsewhere in the app to also take the gate for this change to be safe — the gate only needs to prevent the timeout's own unprompted call from firing concurrently with itself or overlapping a call already resolving on the same page. Extending the gate to guard every navigation call site app-wide is a larger refactor and out of scope here; it can be revisited if the click-freeze recurs after this fix.

**Rejected — a full navigation-wide mutex around every push/pop in the app:** correct in principle but a much larger, riskier change touching every view; not justified until this narrower fix is shown insufficient.

### Decision 3: Per-role default, non-nullable `InactivityTimeoutMinutes` on `User`

**Chosen:** `User.InactivityTimeoutMinutes` is a plain `int`, not nullable. The EF Core migration sets a database-level default of `5`, so any row inserted outside the app (the existing manual-SQL `Sudo`-seeding workflow) gets `5` automatically with no null-handling required anywhere downstream.

`UserService`'s creation path seeds a concrete value from role when the `CreateUserRequest` doesn't specify one:

| Role | Default (minutes) |
|---|---|
| Sudo | 2 |
| Admin | 5 |
| Supervisor | 5 |
| Operator | 10 |
| DispatchingOperator | 20 |

This is stored on the entity at creation time, not recomputed per-login — an admin can subsequently override an individual user's value through the existing update-user endpoint, same as any other field. `CreateUserRequest`/`UpdateUserRequest` gain an optional `int?` field for an explicit override at creation/update time (API-contract nullability only; the entity/column is never null).

`UserDto` (and therefore `LoginResponse.User`) exposes the resolved value. `MainPage.xaml.cs`'s `BtnLogin_Clicked` starts the watcher with `TimeSpan.FromMinutes(response.User.InactivityTimeoutMinutes)`, replacing the hardcoded `TimeSpan.FromMinutes(10)`.

**Confirmed with project owner:** exact role→minutes table above, including the two-tier default (role-based at app-creation time vs. flat `5` DB-level fallback for manually-inserted rows) — the owner's own existing `Sudo` row predates this column and will read as `5` until manually updated; the `2`-minute default only applies going forward to `Sudo` users created through the app.

## Open Questions

None outstanding — all defaults and mechanics above were confirmed directly with the project owner in conversation before this proposal was drafted.
