# Design

## Context

See `proposal.md` - Why. Today `TerminalMode` is purely a per-user resolution (`PermissionService.GetEffectiveTerminalMode`); there is no terminal-identity concept in the auth model at all. The only device-scoped signal that exists, `DeviceName` (`Preferences.Get("DeviceName", DeviceInfo.Name)` on the MAUI client), is currently used solely for the scale device-lock in `WeightController` and carries no auth meaning. `Role` is a single fixed enum per user whose stored ordinal values are frozen by `role-permission-model`'s backward-compatibility requirement, so the new role-rank ordering must be a separate, additive mapping, never a reordering of the enum itself.

## Goals / Non-Goals

**Goals:**
- Resolve `TerminalMode` from a terminal's configured forced role when one exists, ahead of the user's own role/override.
- Gate login at a forced-role terminal by a role rank distinct from the `Role` enum's ordinal values.
- Keep this entirely backward-compatible for any terminal that hasn't been configured with a forced role.

**Non-Goals:**
- No terminal-management UI in this change (see Decisions - Config surface).
- No change to how `CanSelfAuthorizeGate`/`CanCaptureWeightManually` resolve.
- No terminal provisioning workflow beyond auto-registration on first login (see Decisions - Terminal identity).

## Decisions

### Terminal identity: reuse `DeviceName`, auto-register on first login
The client already sends nothing terminal-specific at login today; it will now include its existing `DeviceName` value as the terminal identifier. A `Terminal` row is looked up by `DeviceName`; if none exists, the server auto-creates one with `ForcedRole = null` (unconfigured) rather than rejecting the login. This means:
- No new client-side identity/storage is introduced — `DeviceName` already exists and is already used for the (unrelated) scale device-lock.
- An admin only ever needs to *edit* a terminal's forced role after it already appears (auto-created on its first login), never manually pre-register it.
- **Alternative considered**: a dedicated generated terminal ID provisioned explicitly. Rejected for this change as more moving parts than the problem warrants for a solo-operated, in-production system; `DeviceName` collisions are a known, accepted risk (see Risks).

### Config surface: backend API only, no admin UI yet
A new `TerminalsController` (Admin/Sudo-only, mirroring `user-administration`'s access rule) exposes list + set/clear-forced-role endpoints. No `BasculaUi` screen is built in this change — consistent with how the project already operates (the first `Sudo` user itself is inserted directly against the database, no seeding UI). The in-progress admin portal is a candidate home for this later, but building it now is out of scope.

### Terminal-forced role overrides the user's `TerminalModeOverride` unconditionally
Per the specs, a configured forced role always wins over the user's own role default *and* their `TerminalModeOverride` — there is no per-user exception mechanism to escape a terminal's forced role. This is intentional: the entire point of the feature (per the issue discussion) is that the terminal's rendered workflow must not depend on who is logged in, so an escape hatch here would reopen the exact gap this change closes.

### Role rank is a separate, additive mapping
Implemented as its own ordered lookup (e.g. a `Role -> int` dictionary), not a change to `Role.cs`'s enum ordinals. Order, lowest to highest: `Dispatching Operator`, `Customer Service`, `Operator`, `Supervisor`, `Admin`, `Sudo`.

### Login-denial response is distinguishable from bad credentials
A rank-denied login returns `403 Forbidden` with a distinct error code (e.g. `terminal_role_rank_denied`), never `401 Unauthorized`. The client needs this distinction to show "this terminal isn't available to your role" rather than "wrong username/password" — conflating the two would send operators down the wrong troubleshooting path (retrying a password that was never wrong).

## Risks / Trade-offs

- **[Risk] `DeviceName` is not a guaranteed-stable or guaranteed-unique identifier** (e.g. a device reinstall or a manually changed device name could effectively "orphan" a previously configured terminal, or two devices could coincidentally share a name) → **Mitigation**: accepted for this change given the small, physically-controlled fleet in this deployment; the admin can always re-configure a newly-appearing terminal row. Documented here rather than solved, per the recommendation that drove this decision.
- **[Risk] Auto-registering a `Terminal` row on every unrecognized `DeviceName` could accumulate stale rows** (test devices, renamed devices) → **Mitigation**: rows are cheap, unconfigured (`ForcedRole = null`) rows are functionally inert (no rank gate applies), and cleanup is a manual admin action if it ever matters — not built into this change.
- **[Trade-off] No admin UI means configuring a terminal's forced role requires direct API calls** → accepted per the Config surface decision above; matches existing project operating model.

## Migration Plan

1. Add `Terminal` entity/table (`DeviceName` unique, nullable `ForcedRole`) via EF Core migration — purely additive, no existing table altered.
2. Ship the backend changes (`AuthService`, `PermissionService`, `TerminalsController`) behind the fact that every terminal starts unconfigured (`ForcedRole = null`), so existing behavior is unchanged the moment this deploys.
3. Update the MAUI client to send `DeviceName` at login (it already reads this value for the device lock, so this is passing an existing value to a new place, not introducing new client state).
4. Roll out forced-role configuration terminal-by-terminal, at the admin's own pace, via the new API.

Rollback: revert the client/server deploy; the `Terminal` table and any configured forced roles become inert and can be left in place or dropped without affecting any other data.

## Open Questions

None — the two decisions that would have changed scope (terminal identifier source, config surface) were resolved above rather than deferred.
