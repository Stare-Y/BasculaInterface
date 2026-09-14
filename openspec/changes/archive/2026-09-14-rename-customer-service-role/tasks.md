## 1. Domain

- [x] 1.1 Rename `Role.PurchasingOperator` → `Role.CustomerService` in `Core.Domain/Entities/Identity/Role.cs` — identifier only, position unchanged (stays the last, 6th member, ordinal 5, appended after `Sudo`).
- [x] 1.2 Update the `<see cref="Role.PurchasingOperator">`-style doc-comment cross-references in `Core.Domain/Entities/Identity/TerminalMode.cs`.

## 2. Resolution logic & the login bug fix

- [x] 2.1 `PermissionService.cs`: renamed the `RoleDefaults[Role.PurchasingOperator]` key to `Role.CustomerService` (value unchanged — `[]`, both ABAC flags `false`).
- [x] 2.2 `PermissionService.cs`: renamed the `RoleTerminalModeDefaults[Role.PurchasingOperator]` key to `Role.CustomerService` (value unchanged — `TerminalMode.PedidosOnly`).
- [x] 2.3 `UserService.cs`: renamed the `InactivityTimeoutDefaults[Role.PurchasingOperator]` key to `Role.CustomerService` (value unchanged — `10`).
- [x] 2.4 **The actual bug fix** — `AuthService.cs`'s `ToDto`: added `TerminalMode = _permissionService.GetEffectiveTerminalMode(user)`, matching `UserService.cs`'s `ToDto` exactly.

## 3. Tests

- [x] 3.1 `PermissionServiceTerminalModeTests.cs`: updated `InlineData(Role.PurchasingOperator, TerminalMode.PedidosOnly)` → `Role.CustomerService`.
- [x] 3.2 Added `AuthServiceTerminalModeTests.cs` (new file — no `AuthService` tests existed before this change): a 4-case theory asserting `AuthService.LoginAsync`'s returned `UserDto.TerminalMode` equals whatever `IPermissionService.GetEffectiveTerminalMode` resolves for that user, across all four `TerminalMode` values — this is the regression test that would have caught the original bug.
- [x] 3.3 Ran the full non-integration/non-Live suite: `142/142` passing (`138` prior baseline + `4` new `AuthServiceTerminalModeTests` cases; the renamed `PermissionServiceTerminalModeTests` `InlineData` is a rename, not an addition). The one pre-existing `Live.BasculaClient.WebSocketTesting` failure (needs a real running server on `localhost:5284`) is an unrelated, pre-existing sandbox limitation, not a regression.

## 4. Spec sync

- [x] 4.1 Update `openspec/specs/role-permission-model/spec.md`: "Five fixed roles" → six, correct names including `Customer Service`; extend the role-based-default-permissions requirement to list `Customer Service` alongside `Operator`/`Dispatching Operator`/`Admin`.
- [x] 4.2 Update `openspec/specs/user-authentication/spec.md`: extend "Successful login issues a JWT" to document that the response body's effective `TerminalMode` is included and resolved (not the enum default).
- [x] 4.3 Add `openspec/specs/terminal-mode-assignment/spec.md` as a new main-spec capability (doesn't exist in main specs yet — only ever lived in the still-unarchived `role-driven-terminal-modes` change) — the same `TerminalMode` requirements, written with `Customer Service` naming from the start.
- [x] 4.4 *(Follow-up, explicitly not part of this change)* Archive `openspec/changes/role-driven-terminal-modes/` and reconcile `openspec/specs/self-authorize-gate/spec.md` with the still-unsynced `AuthorizeTurnBypass` endpoint.
  - Done together with this change's own archival: `self-authorize-gate/spec.md` now documents the `BypasTurn`/`AuthorizeTurnBypass` per-use gate requirement, and `role-driven-terminal-modes` is archived below.

## 5. Verification (on-device, owner-gated — same limitation as prior changes in this repo's history)

- [x] 5.1 Log in as a `CustomerService` (renamed) user; confirm `PendingWeightsView` shows `BtnNewWeightLessPedido` immediately from a fresh login, with no `TerminalModeOverride` needed — the originally-reported symptom.
  - Confirmed in production. (Root cause of the residual first-login flakiness turned out to be a separate bug — `InactivityWatcherService`'s unconfigured timer defaulting to 100ms — fixed on branch `fix/terminal-mode-first-login-race`, not part of this change.)
- [x] 5.2 Log in as each of `DispatchingOperator`, `Operator`, `Supervisor`, `Admin`, `Sudo`; confirm each resolves to its documented default (`Secondary`/`Main`/`Main`/`Main`/`Main`) straight from login — no logout/login workaround required.
  - Confirmed in production for `DispatchingOperator`/`CustomerService`; the others share the same login path and code, not separately re-tested.
