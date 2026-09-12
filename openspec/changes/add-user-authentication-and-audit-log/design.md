# Design: User Authentication, Authorization & Audit Log

## Context

The API (`BasculaTerminalApi`) has no authentication or authorization today: `Program.cs` calls `app.UseAuthorization()` but never registers a scheme, and no controller carries `[Authorize]`. The only access control anywhere is `WeightSettings.ChangeProductPasswordHash` — one shared, unsalted SHA-256 hash, checked inline in 7 places across `WeightService.cs` (delete entry, delete detail, change product, change partner, change amount) and `PedidoService.cs` (delete pedido, delete pedido line — added by commit `589d106`). It is not tied to any user (issue #122, an explicit stopgap).

The MAUI terminal's login screen (`MainPage.xaml.cs`) collects no credential — `BtnLogin_Pressed`/`Released` is a timed press-and-hold gesture that reveals hidden checkboxes and navigates on to `PendingWeightsView`/`FinishedWeights`.

This design was scoped via `enrich-issue-for-openspec` against issue #134; the decisions below originated in that enrichment and a follow-up explore pass (marked **Confirmed with project owner**).

## Goals / Non-Goals

**Goals:**
- Real per-user login on the MAUI terminal, with a role + per-user permission model.
- Every mutating API request attributable to a specific user and authorized against their effective permissions.
- Retire the shared-password stopgap in favor of a self-authorize gate tied to real identity.
- Durable, queryable audit trail of every mutating action.
- Add all of the above without changing any existing business logic for a permitted, authenticated user.

**Non-Goals:** `BasculaUi` changes; external IdP/SSO/AD; JWE; refresh tokens; server-side session/last-activity tracking; self-service registration; seeding any account.

## Decisions

### Decision 1: Custom `User` entity, not ASP.NET Core Identity

**Chosen:** A plain `User : BaseEntity` in `Core.Domain/Entities/Identity/`:
```csharp
public class User : BaseEntity {
    public required string Username { get; set; }       // unique
    public required string UserCode { get; set; }        // unique, letters/digits only
    public required string PasswordHash { get; set; }    // salted, see Decision 2
    public required Role Role { get; set; }              // enum: Operator, DispatchingOperator, Supervisor, Admin, Sudo
    public bool? CanSelfAuthorizeGateOverride { get; set; }     // tri-state: null = inherit role default
    public bool? CanCaptureWeightManuallyOverride { get; set; }
}
```
Disabling a user is `IsDeleted = true` (inherited from `BaseEntity`, same convention as every other entity) — a disabled user fails login and fails the gate check.

**Confirmed with project owner:** custom entity, no AD/external IdP — "we can make our own user entity, user password, role."

**Rejected — full `Microsoft.AspNetCore.Identity`:** pulls a large surface (cookie auth, external logins, email confirmation, lockout policies) this project doesn't need, and the owner explicitly asked for a minimal custom entity.

### Decision 2: Password hashing — PBKDF2 via `Microsoft.AspNetCore.Cryptography.KeyDerivation`, not the existing `PasswordHasher.cs`

**Chosen:** A new hasher producing a self-describing string (`{iterations}.{base64 salt}.{base64 subkey}`), using `KeyDerivation.Pbkdf2` (HMACSHA256, ≥100k iterations, 16-byte salt, 32-byte subkey) — one small NuGet package, no full Identity dependency. `Core.Application/Security/PasswordHasher.cs`'s existing SHA-256 hasher is untouched (nothing else uses it after Decision 6 retires its only caller's mechanism — see Decision 6) but is not reused: it is a fixed, unsalted, single-shared-secret hasher, incompatible with per-user salts.

**Confirmed with project owner:** plaintext password now travels client→server (see Decision 6) specifically because a salted hash cannot be verified via client-side string comparison — the owner confirmed this is acceptable given the local-network-only deployment.

### Decision 3: JWT bearer auth, default-authenticated fallback policy

**Chosen:** `Program.cs` adds `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` (HMAC-SHA256, signing key from configuration) and:
```csharp
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build());
```
Every existing GET action across every controller gets `[AllowAnonymous]` explicitly. `SerialPortHub` gets `[AllowAnonymous]` too (SignalR hubs also honor it). `app.UseAuthentication()` is added immediately before the existing `app.UseAuthorization()`.

**Confirmed with project owner:** "Broader: all GET endpoints" exempt; everything else (POST/PUT/PATCH/DELETE) requires auth.

**Why fallback-policy instead of `[Authorize]` on each mutating action:** a fallback (default-deny) policy means any endpoint added after this change is locked down automatically. Opting in per-action risks a forgotten `[Authorize]` on a future mutating endpoint being silently anonymous — the opposite of "add a layer without disturbing current logic," which should mean disturbing nothing *permitted*, not leaving new gaps.

**JWT claims:** `sub` (user id), `unique_name` (username), a custom `usercode` claim, and `role`. Expiry: 12 hours (configurable via a new `WeightSettings.JwtLifetimeHours`) — long enough to outlast a shift given no refresh-token flow exists; the client's own inactivity timer (Decision 7) is what actually forces re-login in practice, not token expiry.

### Decision 4: Role defaults + per-user tri-state ABAC overrides, `Sudo` as true bypass

**Chosen:** A static in-code lookup, `RolePermissionDefaults`:

| Role | CanSelfAuthorizeGate (default) | CanCaptureWeightManually (default) |
|---|---|---|
| Operator | false | false |
| Dispatching Operator | false | false |
| Supervisor | true | true |
| Admin | false | false |
| Sudo | n/a — bypasses all checks | n/a |

`IPermissionService.HasPermission(User user, Permission flag)`:
```
if (user.Role == Role.Sudo) return true;
return (flag == CanSelfAuthorizeGate ? user.CanSelfAuthorizeGateOverride : user.CanCaptureWeightManuallyOverride)
       ?? RolePermissionDefaults[user.Role][flag];
```

**Confirmed with project owner:** "Operator < Dispatching Operator < Supervisor" baseline (Supervisor gets both flags by default, the other two get neither), all flags individually overridable per user regardless of role; `Sudo` is a true bypass — "skips authorization checks entirely, always allowed, everywhere" — not just a role seeded with every flag granted.

**Admin's own defaults:** Admin is not given the two floor-work flags by default — its distinguishing power is user management (Decision 8), not weighing-floor actions. Can be overridden per-user like any other role. Flagged as a reasonable default, not something the owner explicitly specified; easy to revisit since it's a one-line table entry.

### Decision 5: Roles and permission flags live in code, not a database-driven permissions table

**Chosen:** `Role` is a C# enum; the two permission flags are named properties/overrides on `User`, not rows in a generic `Permissions` table. Adding a new permission flag in the future is a migration + a new nullable column, matching how the rest of this codebase evolves its schema (e.g. `WeightDetail.RequiresDisTaring`).

**Rejected — a generic `Role`/`Permission`/`RolePermission`/`UserPermissionOverride` junction-table model:** more flexible (roles/permissions editable at runtime without a deploy) but is real complexity for a system with exactly 2 flags and 5 fixed roles today. Revisit if the permission set grows meaningfully.

### Decision 6: Self-authorize gate replaces `ChangeProductPasswordHash` everywhere it's used

**Chosen:** All 7 gated actions' request DTOs change from `{ PasswordHash: string }` to `{ GateIdentifier: string, GatePassword: string }`. A new `IGateAuthorizationService.TryAuthorizeAsync(identifier, password)`:
1. Look up `User` by `UserCode` (`IsDeleted == false`); if none found, look up by `Username`.
2. If no user resolves, or the password fails verification (Decision 2), return false.
3. Otherwise return `IPermissionService.HasPermission(user, CanSelfAuthorizeGate)`.

`WeightService`'s and `PedidoService`'s 7 call sites replace their `ChangeProductPasswordHash` string-equality check with one call to this service; on `false` they throw the same `UnauthorizedAccessException` they throw today, preserving the existing 400 "Contraseña incorrecta"-style controller behavior (message text may need to become identifier-and-password-agnostic, e.g. "Credenciales inválidas o sin autorización"). `WeightSettings.ChangeProductPasswordHash` is deleted.

**Confirmed with project owner:** "any user, no matter his role, can identify itself to pass the gate" — i.e. the authorizer need not be the currently logged-in operator (a Supervisor can walk up and authorize an Operator's action); resolution order is `UserCode` first, then `Username`, both decided explicitly by the owner. Scope confirmed as "Weight + Pedido/PedidoLine" — the full set commit `589d106` extended to.

### Decision 7: Client-side-only session and inactivity handling

**Chosen:** MAUI adds an `ISessionService` (singleton, DI-registered in `MauiProgram.cs`) holding the decoded post-login payload (user id, username, usercode, role, effective permission flags) in memory, and the raw JWT in `SecureStorage`. An `AuthHeaderHandler : DelegatingHandler`, registered once against the app's `HttpClient`, attaches `Authorization: Bearer <token>` to every request. An `InactivityWatcherService` resets a countdown on user input (implemented as a `TapGestureRecognizer`/pointer hook at the `AppShell` root, reset also on every successful API response) and, on elapse, clears `SecureStorage` + the session singleton and navigates to the login page. The timeout is read from a config value the server also exposes (default 10 minutes) so it can be changed without a client rebuild.

**Confirmed with project owner:** "I like long lived, we can trust them, BUT lets make it, so that, if they are inactive for X time, log out automatically" and, on follow-up, explicitly **client-side only** — no server-side session/last-activity tracking, no sliding server-enforced expiry.

### Decision 8: `CanCaptureWeightManually` fully replaces the device-local toggle

**Chosen:** `EditSettingsView`'s "Capturar peso manualmente" `CheckBox`/`Preferences.Set("ManualWeight", ...)` is removed. Wherever the client currently reads that preference to decide whether manual weight entry is available, it instead reads the logged-in session's `CanCaptureWeightManually` flag.

**Confirmed with project owner:** "Server permission replaces the device toggle" — explicit choice over "both must agree."

### Decision 9: No seeding of any account in this change

**Chosen:** The migration ships with an empty `Users` table. No `Sudo`, no bootstrap `Admin`. The project owner inserts the `Sudo` row directly into the database after deploy; `Sudo` then uses the new admin-only user-management API (Decision 10) to create the first `Admin` and every subsequent user.

**Confirmed with project owner:** "No seeding at all" — reversing an earlier answer that suggested seeding a bootstrap `Admin`.

### Decision 10: Admin-only user-management API, no self-service

**Chosen:** New `UsersController`, gated by the same fallback-authenticated policy plus a role check (`Role == Admin || Role == Sudo`) — the only place in this change a role check happens directly rather than through the two ABAC flags, since user management isn't one of the two defined overridable permissions. Endpoints: create user, update user (role, overrides, password reset), disable user (`IsDeleted = true`).

**Confirmed with project owner:** "Seed one admin, add a manage-users API" (the seeding half was later superseded by Decision 9; the manage-users API half stands).

### Decision 11: Audit log — explicit calls in service methods, not a generic interceptor

**Chosen:** A new `AuditLogEntry : BaseEntity` (`UserId`, `Timestamp` (UTC), `Action` (string, e.g. `"WeightEntry.DeleteSafely"`), `EntityType`, `EntityId`). `IAuditLogService.RecordAsync(...)` is called explicitly at the end of each mutating service method that changes `WeightEntry`/`WeightDetail`/`Pedido`/`PedidoLine` state, resolving the acting user from `IHttpContextAccessor`'s JWT claims. `EntityType`/`EntityId` pairs let the radiography query (Decision 12) filter directly.

**Rejected — a generic EF Core `SaveChanges` interceptor:** would catch everything automatically, but produces an audit trail keyed off raw entity property diffs rather than the meaningful action name (e.g. "DeleteSafely" vs. "an UPDATE that happened to flip IsDeleted") and is harder to review at a glance — this codebase's existing convention (the gate checks themselves) is explicit, inline logic per action, not cross-cutting middleware.

### Decision 12: Radiography query endpoint

**Chosen:** `GET /api/Weight/{id}/Radiography` returns `WeightEntryRadiographyDto`: the `WeightEntry` (regardless of `IsDeleted`), all of its `WeightDetail`s (including logically-deleted ones — queried with `IgnoreQueryFilters()` or an explicit unfiltered query, since `BaseEntity.IsDeleted` already exists), and every `AuditLogEntry` where `EntityType == "WeightEntry" && EntityId == id` or `EntityType == "WeightDetail" && EntityId IN (detail ids)`, ordered by `Timestamp`.

**Confirmed with project owner:** "storage + a basic query endpoint, specially one to receive like the weightentry Id, its details, and get like a full radiography dto, with its details, even the deleted ones, and its audit log section."

## Open Questions

- Exact permission differences between `Dispatching Operator` and `Operator` beyond the shared baseline — the owner named the role but not a concrete behavior difference yet. Ships with identical defaults to `Operator` (Decision 4's table) until specified; the tri-state override mechanism means this can be adjusted per-user without a schema change in the meantime.
- Whether `Pedido`/`PedidoLine` mutations beyond delete (e.g. create/update line, close line) should also sit behind the fallback-authenticated policy's implicit "any authenticated user" bar, or need a specific permission flag of their own — current scope treats all non-GET endpoints identically (any authenticated user may call them; only the 7 gate-list actions need the *additional* `CanSelfAuthorizeGate` check).
