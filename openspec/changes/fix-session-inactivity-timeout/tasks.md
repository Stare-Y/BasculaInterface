## 0. Pre-work — confirmed decisions

- [x] 0.1 Root cause confirmed via code inspection (this proposal's design.md Context): no global activity hook exists (`RegisterActivity()` only called at login); `OnInactivityTimeout`'s `PopToRootAsync()` never reaches the `ModalStack` that nearly every screen in this app is opened on.
- [x] 0.2 Per-role timeout defaults confirmed with project owner: `Sudo`=2, `Admin`=5, `Supervisor`=5, `Operator`=10, `DispatchingOperator`=20 minutes; non-nullable column, DB-level default `5` for rows inserted outside the app.
- [ ] 0.3 Click-freeze exact mechanism NOT confirmed — `WindowsPackageType=None` (unpackaged) rules out Windows PLM suspend/resume; a plain WinUI input-routing quirk on long-idle windows remains an open, separate possibility (see proposal.md Non-goals). This change fixes the two confirmed bugs and defensively guards the leading code-level hypothesis; it does not close that investigation. Left unchecked deliberately — owner still needs to confirm on-device whether it recurs (see 5.4).

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

- [x] 3.1 `AppShell.xaml`: added a `TapGestureRecognizer` on `Shell.GestureRecognizers`, wired in `AppShell.xaml.cs` to call `InactivityWatcherService.RegisterActivity()` (same `MauiProgram.ServiceProvider` DI-resolution pattern as `MainPage.xaml.cs`/`WeightingScreen.xaml.cs`)
- [x] 3.2 `AuthHeaderHandler.SendAsync`: now calls `RegisterActivity()` after any response with `IsSuccessStatusCode == true`; `InactivityWatcherService` added as a constructor dependency (singleton into a transient handler — no lifetime issue)
- [x] 3.3 **Not verified in this sandbox** — a Shell-level `GestureRecognizer` observing rather than consuming taps is the standard MAUI pattern for this and shouldn't interfere with pages' own `Clicked`/`Tapped` handlers, but this project's MAUI/WinUI target cannot be built here (same pre-existing limitation as the parent change's task 7.7 — Wine/Mono XAML-compiler failure, unrelated to this code). **Confirm on a real device** that existing buttons/gestures across the app still behave normally after this change.

## 4. Client — timeout handler fix (⚠️ touches the session-clearing path)

- [x] 4.1 `MainPage.xaml.cs`: `OnInactivityTimeout` now takes a `SemaphoreSlim`-guarded (`WaitAsync(0)`, skip-if-busy) path that drains `Navigation.ModalStack` via `PopModalAsync(animated: false)` in a loop, then calls `PopToRootAsync(animated: false)`
- [x] 4.2 Replaced the hardcoded `TimeSpan.FromMinutes(10)` in `BtnLogin_Clicked` with `TimeSpan.FromMinutes(response.User.InactivityTimeoutMinutes)`

## 5. Verification

- [x] 5.1 `UserServiceInactivityTimeoutTests.cs` — role default for all 5 roles, explicit-value-overrides-default on create, update-without-a-value leaves the existing value untouched, update-with-a-value overwrites it. 8 tests, all passing.
- [x] 5.2 `UsersControllerInactivityTimeoutHttpTests.cs` — creating a user via `POST /api/Users` without an explicit value persists the correct role default (all 5 roles), an explicit value overrides it. **Docker-unverified in this sandbox** — confirmed the failure is the same pre-existing "Docker not reachable" (`Docker.DotNet` ping failure) as every other integration test in this suite, not a code error. Compiles clean against the real `WeightDBContext`/`BasculaApiFactory`. Run `dotnet test --filter Category=Integration` on a machine with Docker/Podman before merging. (The DB-level-default-for-a-raw-SQL-insert half of Decision 3 is not separately integration-tested beyond this — it is EF Core's own documented default-value mechanism, already exercised by this codebase's existing `WeightDetail.IsLoaded` convention, and directly confirmed by inspecting the generated migration in 1.2.)
- [ ] 5.3 Manual/on-device verification (MAUI client cannot be built in this sandbox): confirm activity anywhere in the app resets the timer; confirm timeout from a modal-stack screen (e.g. `WeightingScreen`) actually returns to the login screen; confirm the per-role timeout value takes effect at login for at least two different roles
- [ ] 5.4 Re-run the two disambiguating tests from the original investigation (foreground-only 10+ minute wait; background 10+ minute wait followed immediately by alt-tab) after this fix, to confirm whether the click-freeze recurs — if it does, the WinUI-level hypothesis (task 0.3) becomes the leading explanation and needs its own follow-up

**Overall: backend (sections 1-2) and the client-side code (sections 3-4) are implemented; the backend builds clean (0 errors) and is test-verified (106/106 non-integration, non-"Live" tests passing — 98 pre-existing + 8 new). The new integration test is written but Docker-unverified here, same limitation as the rest of this suite. The MAUI client changes are implemented but entirely unbuilt/unverified in this sandbox — no build is possible here for this target, same as every prior MAUI change in this repo's history. Build/smoke-test on your machine before merging, and specifically re-run the freeze-reproduction steps (5.4) to see whether it's actually resolved.**
