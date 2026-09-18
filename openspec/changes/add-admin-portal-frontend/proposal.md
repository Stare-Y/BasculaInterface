## Why

There is no administrative UI for this system today. Managing `User` accounts requires calling the API directly (Swagger/Postman), and there's no way for Admin/Supervisor staff to browse weight history, search by partner, or inspect a weight entry's audit trail without direct API/DB access. Issue #141 asks for a web portal covering exactly this, enriched and scoped through a prior conversation with the owner (two open business questions — role hierarchy for user management, and the hosting target — were resolved there before this proposal).

## What Changes

- **New React + TypeScript SPA** (`src/frontend/BasculaUi` — already anticipated by name in `openspec/config.yaml`'s project context) replacing the placeholder `src/frontend/frontendhere.txt`. Side-tab navigation: Dashboard, Users, Weights.
- **Login** via the existing `POST /api/Auth/Login`. Only `Admin`, `Supervisor`, `Sudo` may reach the portal past login; other roles are rejected client-side after authenticating.
- **User management** (Admin/Sudo only): create, edit, disable (soft delete) `User` accounts via the existing `Users*` endpoints, unchanged. Supervisor does not see this screen — this matches the existing `user-administration`/`role-permission-model` specs exactly, so **no backend authorization change**.
- **Weight browsing**: active/pending weights (`Weight/Pending`), search by partner (`ClienteProveedor/ByName` → `Weight/All/ByPartner`), and full weight detail (`Weight/ById`) — all existing endpoints, used as-is.
- **Audit on demand**: the weight-detail screen loads the audit trail only when the user explicitly asks, via the existing `Weight/{id}/Radiography` endpoint — already returns exactly this shape, no new endpoint needed.
- **Hosting**: the built SPA is served as static files from `BasculaTerminalApi`'s own `wwwroot`, with an SPA-fallback route, added to `Program.cs`. No IIS, no second process. Chosen because a single Kestrel/Windows-service instance is trivially sufficient at the ~10-concurrent-user scale this needs.
- **Non-disruption constraint (hard requirement, not aspirational):** every existing controller route, DTO shape, `[Authorize]`/`[AllowAnonymous]` attribute, the JWT/CORS/`FallbackPolicy` configuration, and the EF Core model/migrations are unchanged. The only edit to existing backend code is the additive middleware registration in `Program.cs`, ordered so it never intercepts `/api/*` or the SignalR hub route, and the API must start and serve all existing endpoints normally even if the built frontend assets are absent from `wwwroot`.

## Non-goals

- Reusing anything under the old `src/frontend` placeholder — nothing there to reuse.
- Any change to `UsersController` authorization, the soft-delete mechanism, or the `role-permission-model`/`user-administration` specs.
- Any new backend audit, weight-query, or dashboard/stats endpoint — existing endpoints already cover the stated needs; dashboard metrics are computed client-side from existing list responses for now.
- IIS as a hosting target.
- Pagination/total-count metadata on existing list endpoints — a known gap (see design.md), deliberately deferred.

## Impact

**Terminals:** none. This change does not touch the MAUI client (`BasculaInterface`) or any terminal-mode/weighing behavior — it adds a new, separate web consumer of the existing API.

**API:** additive only — new static-file/SPA-fallback middleware in `Program.cs`; zero changes to any controller, DTO, authorization attribute, or migration.

**New code:** `src/frontend/BasculaUi` (React + TypeScript SPA) and a small frontend-build step wired into `BasculaTerminalApi`'s publish pipeline.

**Existing data:** zero impact.
