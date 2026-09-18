# Tasks

## 1. Domain & Data

- [ ] 1.1 Add `Terminal` entity (`DeviceName` unique string, nullable `ForcedRole`) under `Core.Domain/Entities/Identity/` and verify it compiles alongside the existing `User`/`Role` entities.
- [ ] 1.2 Add EF Core migration creating the `Terminals` table with a unique index on `DeviceName`, and verify `dotnet ef database update` applies cleanly against a local dev database with zero rows affected in any existing table.
- [ ] 1.3 Add a `RoleRank` static mapping (`Role -> int`), lowest to highest: `DispatchingOperator`, `CustomerService`, `Operator`, `Supervisor`, `Admin`, `Sudo`, and verify a unit test asserts the exact ordering and that `Role.cs`'s enum ordinals are untouched by this change (diff-check).

## 2. Login & Terminal-Mode Resolution

- [ ] 2.1 Update `AuthService.LoginAsync` to accept a `deviceName` parameter, look up (or auto-create, with `ForcedRole = null`) the matching `Terminal` row, and verify a unit test confirms a login with an unrecognized `deviceName` succeeds and results in exactly one new `Terminal` row.
- [ ] 2.2 Add the rank gate: reject login with `403 Forbidden` / `terminal_role_rank_denied` when the authenticating user's `RoleRank` is below the terminal's `ForcedRole`'s rank, and verify unit tests cover: rank below (denied), rank equal (allowed), rank above (allowed), no forced role configured (no gate applied).
- [ ] 2.3 Update `PermissionService.GetEffectiveTerminalMode` to take the resolved `Terminal` into account: if `ForcedRole` is set, return that role's default `TerminalMode`; otherwise fall back to today's per-user resolution unchanged, and verify unit tests cover both branches plus the existing per-user scenarios still pass unmodified.
- [ ] 2.4 Verify `CanSelfAuthorizeGate`/`CanCaptureWeightManually` resolution is untouched by confirming existing `PermissionService` tests for those two flags pass without modification.

## 3. Admin API

- [ ] 3.1 Add `TerminalsController` with `GET` (list terminals + forced role) and `PUT` (set/clear a terminal's forced role) endpoints, Admin/Sudo-only via the same policy as `UsersController`, and verify a non-admin request returns `403 Forbidden` while an Admin request succeeds.
- [ ] 3.2 Verify end-to-end via an integration test: create a terminal (via login auto-registration), set its forced role through the API, then log in as a user below that rank and confirm `403`/`terminal_role_rank_denied`, and as a user at/above that rank and confirm the returned `TerminalMode` matches the forced role's default.

## 4. MAUI Client

- [ ] 4.1 Update the login request in `BasculaInterface` to include the existing `DeviceName` value (already read via `Preferences.Get("DeviceName", DeviceInfo.Name)` for the scale device-lock) as the terminal identifier, and verify a manual login on a real/emulated device still succeeds and the terminal appears via `GET /api/Terminals`.
- [ ] 4.2 Handle the new `terminal_role_rank_denied` login failure distinctly from bad-credentials, showing a message that the account isn't permitted at this terminal, and verify manually by configuring a terminal's forced role above a test user's rank and attempting login.

## 5. Spec & Documentation Sync

- [ ] 5.1 Run `openspec validate --change add-terminal-forced-role-mode --strict` and resolve any reported issues before implementation is considered complete.
- [ ] 5.2 After implementation, sync the delta specs (`terminal-role-enforcement`, `user-authentication`, `terminal-mode-assignment`) into main specs per `openspec-sync-specs` / archive flow.
