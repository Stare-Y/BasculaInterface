## 0. Pre-work — confirmed decisions

- [x] 0.1 Issue #135 explicitly flagged its own decisions as owner calls, not assumptions — resolved directly in conversation across two follow-up rounds (initial per-toggle questions, then a role-by-role comparison once gaps appeared) rather than via a separate `enrich-issue-for-openspec` pass.
- [x] 0.2 `SecondaryTerminal`/`OnlyPedidos` device settings removed; `DispatchingOperator` becomes the secondary-terminal role; a new 6th role `PurchasingOperator` covers pedidos-only (no existing role fit); `OnlyFinished` reachable only via per-user override (no role default); `BypasTurn` stays a device setting but is gated per-use; `RequirePartner` confirmed dead in production and removed outright.
- [x] 0.3 Investigated `BypasTurn`'s actual call sites: it does not touch `ITurnService` (a ticket-number GET, never blocking) despite the issue's own table describing it that way — it guards `WeightLogisticService`'s single-scale lock via `CanWeight()`. Design.md Decision 3 reflects the real mechanism, not the issue's original guess.

## 1. Domain

- [x] 1.1 Append `PurchasingOperator` to `Core.Domain/Entities/Identity/Role.cs`, **after `Sudo`** (ordinal 5) — do not insert it near `DispatchingOperator` despite reading more naturally there; that would shift every later role's stored int.
- [x] 1.2 New `Core.Domain/Entities/Identity/TerminalMode.cs`: `{ Main, Secondary, PedidosOnly, OnlyFinished }`, doc-commented with a pointer to this change's design.md Decision 1.
- [x] 1.3 `User` gains `public TerminalMode? TerminalModeOverride { get; set; }` — nullable, same tri-state shape as `CanSelfAuthorizeGateOverride`/`CanCaptureWeightManuallyOverride`.

## 2. Resolution logic

- [x] 2.1 `PermissionService.RoleDefaults` (or `IPermissionService`, whichever interface it's declared on) gains an entry for `PurchasingOperator`: `[]` (both ABAC flags `false`), matching `Operator`/`DispatchingOperator`/`Admin`.
- [x] 2.2 `UserService.InactivityTimeoutDefaults` gains an explicit `PurchasingOperator` entry — `10` minutes, matching `Operator` (falls back to `5` via the existing `TryGetValue` guard if skipped, but an explicit entry is clearer).
- [x] 2.3 New `RoleTerminalModeDefaults` dictionary + `GetEffectiveTerminalMode(User user)` method (design.md Decision 1's exact table) — add to `IPermissionService`/`PermissionService` alongside the existing `HasPermission`/`GetRoleDefault`, since it's the same "role default + override" shape. **`Sudo` is NOT special-cased** — resolves to `Main` like everyone else without a specific mapping.
- [x] 2.4 Unit tests: role default for all 6 roles (only `DispatchingOperator`→`Secondary` and `PurchasingOperator`→`PedidosOnly` differ from `Main`), override wins regardless of role for all 4 `TerminalMode` values, `Sudo` resolves to `Main` with no override (not bypassed). `PermissionServiceTerminalModeTests.cs`, 12/12 passing.

## 3. Backend — DTOs & user CRUD

- [x] 3.1 `UserDto` gains `TerminalModeOverride` (raw, `TerminalMode?`) and `TerminalMode` (resolved, computed the same way `CanSelfAuthorizeGate`/`CanCaptureWeightManually` already are in `UserService.ToDto`).
- [x] 3.2 `CreateUserRequest`/`UpdateUserRequest` gain `TerminalMode? TerminalModeOverride = null`; `UpdateUserRequest` also gains `bool ResetTerminalModeOverride = false`, mirroring the two ABAC override/reset pairs exactly.
- [x] 3.3 `UserService.CreateAsync`/`UpdateAsync`: wire `TerminalModeOverride` the same way the two ABAC overrides are already wired (reset-then-explicit-value-then-leave-untouched ordering in `UpdateAsync`).
- [x] 3.4 Generated `20260913100328_AddUserTerminalModeOverride.cs` — verified plain nullable `AddColumn<int>`, no default, no data migration. No migration needed for the `Role` enum's new member itself (plain int-backed, no check constraint).

## 4. Backend — turn-bypass gate endpoint

- [x] 4.1 New `[HttpPost("AuthorizeTurnBypass")]` on `WeightController`, request body `{ GateIdentifier, GatePassword }`, calling `IGateAuthorizationService.TryAuthorizeAsync` directly (new constructor dependency) and returning the bool — no mutation, no new service method needed.
- [x] 4.2 `WeightControllerAuthorizeTurnBypassTests.cs` (3 tests) pins the controller's wiring; the resolve/verify/permission logic itself is already covered by the existing `GateAuthorizationServiceTests.cs`. 138/138 full suite passing.

## 5. Client — remove the four device Preferences

- [x] 5.1 `EditSettingsView.xaml`/`.xaml.cs`: removed `CheckBoxSecondaryTerminal`, `CheckBoxOnlyPedidos`, `CheckBoxOnlyFinished`, `CheckBoxRequirePartner` (markup, code-behind handlers, `LoadPreferences`/`SetPreferences` calls). The `OnlyFinished`-triggers-mutual-exclusion block (`CheckBoxOnlyFinished_CheckedChanged`) was removed entirely along with it — nothing left to be exclusive with once those 4 checkboxes are gone; only `BypasTurn` remains from that original group.
- [x] 5.2 Rewired every remaining read of the four removed keys to the resolved `TerminalMode`:
  - `SecondaryTerminal`: `Models/WeightEntryDetailRow.cs` (×2), `ViewModels/BasculaViewModel.cs`, `ViewModels/DetailedWeightViewModel.cs`, `Views/WeightingScreen.xaml.cs` (×3 — found a 4th on closer inspection, `OnAppearing`'s `EntryVehiclePlate.IsEnabled`, also fixed), `Views/DetailedWeightView.xaml.cs` (×3), `Views/PendingWeightsView.xaml.cs`
  - `OnlyPedidos`: `Views/DetailedWeightView.xaml.cs` (×4), `Views/PendingWeightsView.xaml.cs` (×2)
  - `OnlyFinished`: `MainPage.xaml.cs`, `Models/WeightEntryDetailRow.cs`
  - Used the codebase's own existing static-resolution pattern (`MauiProgram.ServiceProvider.GetService(typeof(ISessionService))`, already used by `MainPage`/`WeightingScreen`) rather than constructor injection — several touched classes (`BasculaViewModel`, `DetailedWeightViewModel`, `DetailedWeightView`, `PendingWeightsView`) have a second parameterless constructor for design-time/no-DI-arg construction, which constructor injection would've complicated for no benefit over the pattern already established here.
- [x] 5.3 `ViewModels/BasculaViewModel.cs`'s `ValidateBeforePosting`: removed `Preferences.Get("RequirePartner", false) ||` from the condition, leaving `if (Providers) { ... }` untouched; also fixed a stale comment referencing the removed key elsewhere in the same file.
- [x] 5.4 Confirmed via grep: zero live `Preferences.Get("SecondaryTerminal"|"OnlyPedidos"|"OnlyFinished"|"RequirePartner", ...)` calls remain anywhere in `BasculaInterface` — the only surviving hit is a pre-existing, already-commented-out line in `PendingWeightsViewModel.cs` (dead code predating this change, left untouched).

## 6. Client — gate BypasTurn's actual use

- [x] 6.1 New reusable `Views/PopUps/AuthorizeTurnBypassPopUp` (mirrors `ChangePartnerConfirmPopUp`'s exact structure/pattern, generic message since there's no per-action name to show) plus `BasculaViewModel.AuthorizeTurnBypassAsync` (calls the new endpoint, never throws). Wired at all three real gate points, each via a small `TryAuthorizeTurnBypassAsync` helper in the owning code-behind:
  - `Views/DetailedWeightView.xaml.cs`: the `CanWeight()` gate at two call sites, plus a third, distinct gate discovered on closer inspection — the "row already claimed by another device" (`WeightedBy` mismatch) check in `CollectionViewWeightDetails_SelectionChanged`, which `BypasTurn` also silently skipped before this change and is now gated the same way.
  - `Views/PendingWeightsView.xaml.cs`: the one `CanWeight()` gate.
  - `Views/WeightingScreen.xaml.cs`'s own `BypasTurn` read (skipping the keep-alive renewal loop) was deliberately left untouched — it doesn't itself bypass any check, it just skips maintaining a lock renewal that wouldn't matter if the operator isn't respecting the lock in the first place.
- [ ] 6.2 Manual/on-device test: `BypasTurn` on, scale locked by another terminal — correct credential proceeds, wrong/missing credential is blocked exactly like today's default (no-bypass) behavior. **Not run** — requires the real Windows machine/hardware, same as every other on-device verification task in this suite.

## 7. Verification

- [x] 7.1 Full non-integration/non-Live test suite passes: 138/138 (123 pre-existing + 12 `PermissionServiceTerminalModeTests` + 3 `WeightControllerAuthorizeTurnBypassTests`).
- [ ] 7.2 Docker-dependent HTTP integration coverage for `AuthorizeTurnBypass` and the `TerminalModeOverride` create/update path was **not added** — same sandbox limitation as every other integration test in this suite means it couldn't be verified here even if added. The controller-level wiring test (4.2) and the resolution-logic tests (2.4) already cover the actual logic; this would only add an HTTP-round-trip layer. Left for a future pass on a machine with Docker/Podman, alongside the other already-outstanding Docker-unverified tests.
- [ ] 7.3 MAUI/WinUI client changes (sections 5-6) are not build-verifiable in this sandbox (no MAUI workloads) — owner builds and smoke-tests on the real machine, same limitation as every prior client change this session. Every edit was made by reading the actual surrounding code first (not guessed), and cross-checked against the codebase's own existing patterns (`ISessionService` static resolution, `ChangePartnerConfirmPopUp`'s structure) rather than introduced fresh.
- [ ] 7.4 Manual/on-device confirmation: a `DispatchingOperator` login shows secondary-terminal behavior with no device setting involved; a `PurchasingOperator` login shows pedidos-only behavior; a user with `TerminalModeOverride = OnlyFinished` shows finished-only behavior regardless of role; `EditSettingsView` no longer shows the four removed checkboxes; `BypasTurn` prompts for and enforces a gate credential (6.2).

**Overall: backend fully implemented and verified (138/138 tests, 0 warn/0 err across all buildable projects). Client-side (MAUI/WinUI) fully implemented but not build-verifiable in this sandbox — same limitation as every prior client change this session — and needs the owner's on-device confirmation (6.2, 7.4) plus a real build to catch anything a careful read-through couldn't. Remaining open items are 7.2 (Docker-unverified HTTP integration test, not added) and 7.3/7.4 (owner-gated on-device verification), consistent with every other change in this repo's history.**

## 8. Follow-up fix from on-device testing — CanWeight/ReleaseWeight missing [AllowAnonymous]

Discovered on-device immediately after deploying this change: a fresh first-time login intermittently got `401 Unauthorized` on `PUT /api/Weight/CanWeight` (confirmed via server log with `Microsoft.AspNetCore` bumped to `Debug`: `"AuthenticationScheme: Bearer was not authenticated"`), which `BasculaViewModel.CanWeight()`'s catch-all silently turned into a misleading "bascula ocupada." Logging out and back in worked around it, but the owner correctly identified this as wrong: `CanWeight`/`ReleaseWeight` are pure device-coordination primitives (which terminal owns the single scale, keyed by `deviceId`) with no relation to user identity — the same category as the bascula websocket, which is already `[AllowAnonymous]`. `git log` confirmed these two actions never carried that attribute since the fallback-authenticated policy was first introduced — a pre-existing gap in the original auth work, not a regression from this change, just surfaced by it.

- [x] 8.1 Added `[AllowAnonymous]` to `WeightController.RequestWeight` (`CanWeight`) and `ReleaseWeight`.
- [x] 8.2 Updated `openspec/specs/authorization-policy/spec.md` — new ADDED-equivalent requirement documenting these two endpoints are unauthenticated, since the master spec's existing "every GET endpoint" requirement didn't cover these (they're PUT).
- [x] 8.3 138/138 tests still passing after the fix.
- [x] 8.4 Confirmed by the owner on-device: a fresh first-time login no longer needs a second log-in/out cycle to reach the weighing screen.
