## Why

Issue #134 asks for real user identity and an audit trail. Today the API has no authentication at all — `Program.cs` never registers an auth scheme, no controller carries `[Authorize]` — and the only access control anywhere is `WeightSettings.ChangeProductPasswordHash`: one shared, unsalted password hash checked inline before a handful of sensitive mutations, explicitly documented in code as an "insecure-by-design stopgap" (issue #122). The MAUI terminal's login screen (`MainPage.xaml.cs`) is not a real login either — `BtnLogin_Pressed`/`BtnLogin_Released` runs a timed press-and-hold gesture that reveals hidden checkboxes; no credential is ever collected. There is no way to know which operator performed a given action.

This proposal was scoped through `enrich-issue-for-openspec` against issue #134, resolving business decisions (roles, gate mechanics, session behavior, bootstrap, audit scope) with the project owner before drafting — see design.md Decisions.

## What Changes

- **New `User` entity** (`Username`, unique alphanumeric `UserCode`, salted password hash, `Role`) — no external identity provider, no seeding of any kind in this change (the project owner inserts the first `Sudo` row manually).
- **Real login on the MAUI terminal only** (`BasculaInterface`) — `BasculaUi` (React) is untouched. Login resolves the typed identifier by trying `UserCode` first, then `Username`, then verifies the password; issues a JWT.
- **Global default-authenticated API**: every GET endpoint, on every controller, plus the `SerialPortHub` websocket, stays `[AllowAnonymous]`. Every mutating request (POST/PUT/PATCH/DELETE) requires a valid, authorized session — enforced via a fallback authorization policy, not per-endpoint opt-in, so anything added later is locked down by default.
- **Hybrid RBAC/ABAC permissions**: five roles (`Operator`, `Dispatching Operator`, `Supervisor`, `Admin`, `Sudo`). Each role has default values for two permission flags (`CanSelfAuthorizeGate`, `CanCaptureWeightManually`); either flag can be overridden per user regardless of role. `Sudo` bypasses every authorization check unconditionally.
- **Self-authorize gate replaces the shared password** entirely, across all 7 of its current call sites (`WeightService`: delete entry, delete detail, change product, change partner, change amount; `PedidoService`: delete pedido, delete pedido line — the set extended by commit `589d106`). The acting authorizer presents their own identifier + **plaintext** password (a deliberate change from today's client-hashed value, required because a per-user salted hash can't be verified by client-side string comparison); the action proceeds only if that resolves to a user with `CanSelfAuthorizeGate` (or `Sudo`). `ChangeProductPasswordHash` is deleted.
- **Client-enforced session**: long-lived login, JWT stored in `SecureStorage`, attached via an `HttpClient` handler. A configurable inactivity timer (default 10 minutes) clears the session client-side only — no server-side session tracking.
- **`CanCaptureWeightManually` replaces the device-local "Capturar peso manualmente" `Preferences` toggle** outright — the per-user permission is the only gate going forward.
- **Admin-only user management API** to create/edit users, roles, and per-user overrides. No bootstrap seeding.
- **Audit log**: every mutating action is durably recorded (actor, timestamp, action, target entity/id). A query endpoint returns a full "radiography" for a `WeightEntry` — the entry, its details (including logically-deleted ones, via existing `BaseEntity.IsDeleted`), and every audit entry recorded against that entity and its details.

## Capabilities

### New Capabilities
- `user-authentication`: `User` entity, identifier resolution, salted password verification, JWT issuance.
- `authorization-policy`: default-authenticated fallback policy; explicit exemptions for GET endpoints and the websocket.
- `role-permission-model`: role defaults, per-user tri-state ABAC overrides, `Sudo` bypass.
- `self-authorize-gate`: replaces `ChangeProductPasswordHash` on all existing gated Weight/Pedido actions.
- `session-management`: MAUI login UI, token storage, Bearer attachment, client-side inactivity auto-logout, retirement of the device-local manual-weight toggle.
- `user-administration`: admin-only CRUD for users, roles, and permission overrides.
- `audit-log`: durable action logging plus the `WeightEntry` radiography query endpoint.

### Removed Capabilities
- The shared, unsalted `ChangeProductPasswordHash` gate (config value, and its 7 inline call sites) is deleted outright, not deprecated in place.
- The MAUI-side device-local "Capturar peso manualmente" `Preferences` toggle is removed; superseded by the per-user `CanCaptureWeightManually` flag.

## Non-goals

- `BasculaUi` (React dashboard) — no login, no token enforcement; a future change.
- Any change to the `SerialPortHub` websocket's access model — it stays fully open.
- Full ASP.NET Core Identity, external IdP, SSO, or Active Directory integration.
- JWE, refresh tokens, or server-side session/last-activity tracking.
- Self-service registration.
- Any change to weighing/pedido/print business logic for a permitted, authenticated user.

## Impact

**Affected terminals:** MAUI terminal (`BasculaInterface`) only — all terminal modes (main, secondary, "Solo Pedidos"), since the login and gate changes are app-wide, not mode-specific. `BasculaUi` unaffected.
**API:** New `AuthController` (login), `UsersController` (admin-only management), a new audit/radiography query endpoint; `[Authorize]` fallback policy added to `Program.cs`; `WeightController` and `PedidoController` lose their `PasswordHash`-based gated request bodies in favor of an identifier+password gate credential.
**Domain/Service:** New `User`, `AuditLogEntry` entities; new `IAuthService`, `IUserService`, `IPermissionService`, `IAuditLogService`. `WeightService`/`PedidoService`'s 7 inline password comparisons are replaced by a single gate-check call.
**Database:** New migration — `Users` table (unique `Username`, unique `UserCode`), `AuditLogEntries` table. No seed data.
**Client:** `BasculaInterface` — `MainPage` gets a real login form; a new session service, auth `HttpClient` handler, and inactivity watcher; gated-action popups collect identifier+password instead of the shared password; `EditSettingsView`'s "Capturar peso manualmente" checkbox is removed.
