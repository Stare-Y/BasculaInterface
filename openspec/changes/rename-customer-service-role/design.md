# Design: Rename Customer Service Role & Fix Login Terminal Mode

## Context

Investigating why a newly-created `PurchasingOperator` user still saw `Operator`-style UI in `PendingWeightsView` (`BtnNewWeightLessPedido` missing) traced through the full role → login → session → view chain and found the actual bug sits one layer upstream of where it was first suspected: not in the view's `TerminalMode` check itself, but in what the login response ever set that field to. That investigation also surfaced the naming mismatch the project owner then asked to correct, and the fact that `role-driven-terminal-modes` shipped its code without ever being archived, leaving its specs unsynced.

## Goals / Non-Goals

**Goals:** fix the login path so `TerminalMode` is actually the resolved, role/override-derived value for every role; rename `PurchasingOperator` to `CustomerService` with identical behavior; reaffirm `DispatchingOperator`/`Secondary` and override-as-exception as documented invariants; sync the specs this change actually touches.

**Non-Goals:** any new "purchasing movements" permission (none exists, none needed); `BypasTurn`/`AuthorizeTurnBypass` mechanics or the separate staleness in `self-authorize-gate/spec.md`; archiving `role-driven-terminal-modes` itself; any DB migration.

## Decisions

### Decision 1: Fix lives in `AuthService.ToDto`, not in `UserDto`'s default or the client

**Chosen:** add `TerminalMode = _permissionService.GetEffectiveTerminalMode(user)` to `AuthService.ToDto`, mirroring `UserService.ToDto` exactly. Keeps "who computes the effective mode" single-sourced in `PermissionService`.

**Rejected — give `UserDto.TerminalMode` a non-`Main` default:** meaningless, since the correct value is per-user, not a fixed constant.

**Rejected — derive `TerminalMode` from `Role` client-side:** would resurrect a client-side copy of role-default logic — exactly what `role-driven-terminal-modes` moved away from device `Preferences` to avoid. The server must remain the single source of the effective value, same as the two ABAC flags.

### Decision 2: Rename in place (same ordinal), not deprecate-and-add

**Chosen:** rename only the C# identifier. `Role` is confirmed stored as a plain `integer` column (migration snapshot; no `HasConversion<string>` anywhere in the DbContext), so this changes zero stored data — any row currently holding the int previously named `PurchasingOperator` resolves, after this change, to `CustomerService` with identical role-derived behavior (same `TerminalMode`, same ABAC defaults, same inactivity default). No migration.

**Rejected — add a new `CustomerService` member and deprecate `PurchasingOperator`:** would need a data migration to move any existing rows over and leaves a permanently-dead enum member for no benefit, since only the ordinal (not the C# name) is ever persisted.

### Decision 3: `DispatchingOperator`/`Secondary` and override-as-exception are documented, not re-implemented

**Chosen:** no code change — `PermissionService.RoleTerminalModeDefaults[Role.DispatchingOperator] = TerminalMode.Secondary` already matches what was asked for. What's added is explicit spec language stating the override exists for one-off terminal exceptions and that role assignment — not the override — is the primary, production way to configure a terminal's behavior. This is called out because it's exactly the kind of implicit-knowledge gap that let `role-driven-terminal-modes` ship without ever getting its specs synced.

### Decision 4: Sync three spec files now; defer archiving `role-driven-terminal-modes`

**Chosen:** update `role-permission-model/spec.md` and `user-authentication/spec.md` in place (`MODIFIED`), and add `terminal-mode-assignment/spec.md` as a new main-spec capability (it never existed outside the still-unarchived change) — completing the sync `role-driven-terminal-modes` should have done, using the corrected `CustomerService` name from the start so it isn't synced once under the old name and immediately re-synced.

**Rejected — archive `role-driven-terminal-modes` as part of this change:** conflates a functional bug-fix/rename with unrelated process housekeeping (that change also touches `self-authorize-gate`'s `AuthorizeTurnBypass`, which remains unsynced and is out of scope here). Left as a recommended follow-up.

## Open Questions

None blocking. Recommended follow-up (not part of this change): archive `openspec/changes/role-driven-terminal-modes/` and reconcile `openspec/specs/self-authorize-gate/spec.md` with the still-unsynced `AuthorizeTurnBypass` endpoint — both pre-existing gaps, unrelated to the fixes here.
