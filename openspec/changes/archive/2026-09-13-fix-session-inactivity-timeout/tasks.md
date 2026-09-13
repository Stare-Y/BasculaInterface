## 0. Pre-work — confirmed decisions

- [x] 0.1 Root cause confirmed via code inspection (this proposal's design.md Context): no global activity hook exists (`RegisterActivity()` only called at login); `OnInactivityTimeout`'s `PopToRootAsync()` never reaches the `ModalStack` that nearly every screen in this app is opened on.
- [x] 0.2 Per-role timeout defaults confirmed with project owner: `Sudo`=2, `Admin`=5, `Supervisor`=5, `Operator`=10, `DispatchingOperator`=20 minutes; non-nullable column, DB-level default `5` for rows inserted outside the app.
- [x] 0.3 Click-freeze exact mechanism still NOT diagnosed — `WindowsPackageType=None` (unpackaged) rules out Windows PLM suspend/resume; a plain WinUI keyboard/input-focus bug on an unfocused window is confirmed real (reproduced independently on a *different* screen, `DetailedWeightView`, with no timeout involved at all — a plain `Entry` accepted clicks but not keystrokes, and a row-click navigation hung forever with no error). **Not fixed at the root** — see section 6: the timeout's own navigation is now deferred until the window regains focus, which sidesteps this bug for that one trigger, but the underlying WinUI issue can still surface any other time the window loses focus, unrelated to the timeout entirely. Checked off because the investigation concluded with a mitigation decision, not because the root cause was found.

## 1. Domain & database

- [x] 1.1 Added `InactivityTimeoutMinutes` (non-nullable `int`) to `Core.Domain/Entities/Identity/User.cs`
- [x] 1.2 Generated `Infrastructure/Migrations/20260913073605_AddUserInactivityTimeout.cs` (`dotnet ef migrations add`, same throwaway-connection-string pattern as the parent change). Verified: `AddColumn<int>(..., nullable: false, defaultValue: 5)` — the default is a real SQL-level `DEFAULT 5`, not just a C#-side fallback. Also added `WeightDBContext.OnModelCreating`'s `HasDefaultValue(5)` for the property, matching the existing `WeightDetail.IsLoaded` convention (EF omits the column from its INSERT when the CLR value is left at its type default, letting the DB default apply)
- [x] 1.3 Added a private static `InactivityTimeoutDefaults` role→minutes lookup in `UserService.cs` (mirrors `PermissionService`'s co-located `RoleDefaults` pattern rather than a separate top-level class) — consulted only at creation time, with a `TryGetValue`-guarded fallback to `5` if a role is ever missing from the table

## 2. Backend — user creation & DTOs

- [x] 2.1 `CreateUserRequest`/`UpdateUserRequest` gained an optional `int? InactivityTimeoutMinutes = null` (API-contract nullability only; the entity/column itself is never null)
- [x] 2.2 `UserService.CreateAsync`: uses the request's explicit value if supplied, otherwise seeds from `InactivityTimeoutDefaults[request.Role]` (with the `5`-fallback guard from 1.3)
- [x] 2.3 `UserService.UpdateAsync`: updates the value only if the request supplies one; otherwise leaves the existing stored value untouched
- [x] 2.4 `UserDto` (and therefore `LoginResponse.User`) now exposes `InactivityTimeoutMinutes`

## 3. Client — activity tracking

- [x] 3.1 First attempt (`AppShell.xaml`'s `<Shell.GestureRecognizers>`) **failed to compile** on the owner's machine: "No property, BindableProperty, or event found for GestureRecognizers" — `Shell`/`Page` doesn't host that member, only `View`/`Layout` types do. Not caught here since this sandbox cannot build the MAUI/WinUI target at all. **Fixed**: reverted `AppShell.xaml`/`.xaml.cs` to their original state; the hook now lives in `MauiProgram.cs`'s existing `#if WINDOWS` `OnWindowCreated` callback, via `window.Content.AddHandler(UIElement.PointerPressedEvent, handler, handledEventsToo: true)` on the native WinUI window — see design.md Decision 1's revision.
- [x] 3.2 `AuthHeaderHandler.SendAsync`: now calls `RegisterActivity()` after any response with `IsSuccessStatusCode == true`; `InactivityWatcherService` added as a constructor dependency (singleton into a transient handler — no lifetime issue)
- [x] 3.3 **Verified on the owner's machine**: 3.1's revised fix compiles clean and existing buttons/gestures behave normally with the handler attached.

## 4. Client — timeout handler fix (⚠️ touches the session-clearing path)

- [x] 4.1 `MainPage.xaml.cs`: `OnInactivityTimeout` now takes a `SemaphoreSlim`-guarded (`WaitAsync(0)`, skip-if-busy) path that drains `Navigation.ModalStack` via `PopModalAsync(animated: false)` in a loop, then calls `PopToRootAsync(animated: false)`
- [x] 4.2 Replaced the hardcoded `TimeSpan.FromMinutes(10)` in `BtnLogin_Clicked` with `TimeSpan.FromMinutes(response.User.InactivityTimeoutMinutes)`

## 5. Verification

- [x] 5.1 `UserServiceInactivityTimeoutTests.cs` — role default for all 5 roles, explicit-value-overrides-default on create, update-without-a-value leaves the existing value untouched, update-with-a-value overwrites it. 8 tests, all passing.
- [x] 5.2 `UsersControllerInactivityTimeoutHttpTests.cs` — creating a user via `POST /api/Users` without an explicit value persists the correct role default (all 5 roles), an explicit value overrides it. **Docker-unverified in this sandbox** — confirmed the failure is the same pre-existing "Docker not reachable" (`Docker.DotNet` ping failure) as every other integration test in this suite, not a code error. Compiles clean against the real `WeightDBContext`/`BasculaApiFactory`. Run `dotnet test --filter Category=Integration` on a machine with Docker/Podman before merging. (The DB-level-default-for-a-raw-SQL-insert half of Decision 3 is not separately integration-tested beyond this — it is EF Core's own documented default-value mechanism, already exercised by this codebase's existing `WeightDetail.IsLoaded` convention, and directly confirmed by inspecting the generated migration in 1.2.)
- [x] 5.3 Manual/on-device verification: confirmed working by the owner — modal-stack logout, per-role timeout values, and general behavior all check out.
- [x] 5.4 Re-tested the focus/freeze scenario after the section 6 mitigation: timeout-while-unfocused no longer traps the app — the deferred navigation runs cleanly the moment focus returns. Confirmed working by the owner. The underlying WinUI focus bug itself (task 0.3) remains undiagnosed and can still occur outside this one trigger.

## 6. Follow-up fix — defer the timeout's navigation until the window regains focus

Discovered during on-device testing of section 4-5: the owner reproduced the click-freeze independent of any timeout (task 0.3), and separately confirmed that *our* timeout-triggered navigation itself got the app trapped when it fired while the window was unfocused (logout succeeded server-side/session-wise, but the UI never became interactive again). Not part of the original proposal — added once the on-device behavior made the gap concrete.

- [x] 6.1 `InactivityWatcherService` gained `IsWindowActive` (bool) and an `OnWindowActivated` event, set via a new `NotifyWindowActivationChanged(bool)` method.
- [x] 6.2 `MauiProgram.cs`'s existing `#if WINDOWS` `OnWindowCreated` callback now also subscribes to the native `window.Activated` event and forwards `WindowActivationState != Deactivated` into 6.1.
- [x] 6.3 `MainPage.OnInactivityTimeout` still clears the session immediately regardless of focus (the real security boundary — any API call made in the meantime already fails unauthenticated). The actual `PopModalAsync`/`PopToRootAsync` navigation (extracted into `PerformLogoutNavigationAsync`) now only runs immediately if `IsWindowActive`; otherwise a `_logoutNavigationPending` flag is set and run by a new `OnWindowActivated` subscription (wired in the constructor) the moment focus returns.
- [x] 6.4 Verified on the owner's machine (5.4) — the app is no longer trapped after a timeout that fires while unfocused.
- [ ] 6.5 **Not done** — the root WinUI focus/keyboard-input bug itself is still undiagnosed. This only removes one trigger for it (our own forced navigation); the same symptom (clicks/keys not registering while hover still works) can still occur from the window simply losing focus for any other reason, as already observed once on `DetailedWeightView` with no timeout involved. Worth its own investigation if it recurs — candidates raised in conversation: the custom `OverlappedPresenter`/title-bar setup in `MauiProgram.cs`, or a Windows App SDK/WinUI version update.

## 7. Related fix from the same testing session — weight-scale turn lock (different capability, bundled here for traceability)

Not part of this change's original scope (`WeightLogisticService`/the self-authorize gate's turn lock predates `add-user-authentication-and-audit-log` entirely) — the owner hit it while testing this change and asked for it to be fixed in the same pass. Recorded here rather than a separate change since it was small, self-contained, and already fixed+tested before this document was updated.

- [x] 7.1 `WeightLogisticService.KeepTurnAliveAsync`'s auto-expiry reset now runs under the same semaphore as `RequestWeight`/`ReleaseWeight`, and only resets the turn it was actually created for (`ReferenceEquals` against the `CancellationTokenSource` captured at launch) — closes a data race where a stale/superseded timer could clobber a freshly renewed or newly granted turn.
- [x] 7.2 `WeightingScreen` now awaits any in-flight heartbeat renewal (bounded to 5s) before calling `ReleaseWeight()` on exit — closes the race where a renewal already in flight could land *after* the release and silently re-grant the turn to a session that had already ended (the concrete mechanism behind the phantom "Bascula ocupada" reports).
- [x] 7.3 Verified: existing `WeightLogisticServiceTests.cs` (7 tests) still pass unchanged; full non-integration suite (106/106) passes. No new timing-based regression test added — `_turnTimeout` is a hardcoded 10s, making a deterministic, fast test impractical without also making it configurable, which wasn't asked for.
- [x] 7.4 Confirmed working by the owner on-device.
- [x] 7.5 **Resolved, not a bug**: confirmed with the owner — this API instance only ever manages one physical scale, so a single global lock is the correct, intended semantics (it's meant to serialize every customer down to one at a time). No scoping change needed.

**Overall: fully implemented and confirmed working on the owner's machine, including the section 6 and 7 follow-ups. Remaining open items are 5.2 (Docker-unverified integration test, same sandbox limitation as the rest of this suite) and 6.5 (the root WinUI focus bug, undiagnosed — accepted as a known limitation unless it recurs).**
