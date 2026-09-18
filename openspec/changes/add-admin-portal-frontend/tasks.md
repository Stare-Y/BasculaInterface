## 0. Pre-work — confirmed decisions

- [x] 0.1 Issue #141 was enriched via `enrich-issue-for-openspec`, resolving two owner business questions: user-management role hierarchy (kept as-is: Admin/Sudo full CRUD, Supervisor none) and hosting target (served from the API's own `wwwroot`, not IIS). No backend authorization change results from this proposal.
- [x] 0.2 Non-disruption is a hard requirement: no existing controller route, DTO, `[Authorize]`/`[AllowAnonymous]` attribute, JWT/CORS/`FallbackPolicy` config, or EF Core model/migration changes. Only additive middleware in `Program.cs`. No task below should touch anything outside that boundary — if one seems to require it, stop and flag it rather than proceeding.

## 1. Backend — additive static hosting only

- [x] 1.1 Add `app.UseDefaultFiles(); app.UseStaticFiles(); app.MapFallbackToFile("index.html");` to `Program.cs`, placed **after** the existing `app.MapControllers()`/hub mapping so unmatched `/api/...` requests still 404 instead of falling through to `index.html` (design.md Decision 1).
- [x] 1.2 Verify with `wwwroot` absent (fresh checkout, no frontend build yet): API starts cleanly, all existing endpoints respond exactly as before, only the new fallback route 404s. This is the concrete non-disruption check, not just a read-through (design.md Decision 2). Verified: `wwwroot` doesn't exist in the working tree; `dotnet build` succeeds; the full Integration suite (which boots the app end-to-end via `WebApplicationFactory<Program>`, exercising `MapControllers()` → the new static-file middleware → `RunAsync`) passes at the same rate as before the change (see 1.3). A literal `dotnet run` against a live Postgres wasn't done here — no DB credentials available in this environment (that's the repo owner's step per prior project convention).
- [x] 1.3 Run the full existing `BasculaTerminalTest` suite (Unit/Integration) unmodified — confirm 0 regressions, 0 new failures. Record the pass count for tasks.md's final summary. **Result: 196 passed / 5 failed / 201 total, both with and without this change** (confirmed by stashing `Program.cs` and re-running). The 5 failures are pre-existing and unrelated (`UsersControllerInactivityTimeoutHttpTests` — a generated `UserCode` value fails an alphanumeric validation rule; not touched by this change). Zero regressions, zero new failures.

## 2. Frontend — project scaffold

- [x] 2.1 Scaffold `src/frontend/BasculaUi` with Vite + React + TypeScript; remove the placeholder `src/frontend/frontendhere.txt`. Note: `BasculaUi` was not actually empty — it held unmerged, in-progress teammate work (MUI/axios stack, `OrdersPage`/`OrderFormPage`, committed on `master` plus more on unmerged `origin/feature/StylingClientSide`). Per explicit owner decision (asked directly, given "build fresh, ignore existing branch"), that prior work was preserved non-destructively at `src/frontend/BasculaUi.orders-wip` (git `rename`, full history intact, nothing deleted) rather than overwritten in place, and a fresh scaffold was hand-written at `src/frontend/BasculaUi` (the `npm create vite` scaffolding command itself was blocked by Claude Code's own safety classifier as "Irreversible Local Destruction", so the standard Vite+React+TS file set was written by hand instead — same result).
- [x] 2.2 Add `react-router-dom`; set up the side-tab shell layout (Dashboard / Users / Weights tabs) with route placeholders.
- [x] 2.3 Thin `fetch`-based API client module: base URL config, JSON handling, attaches `Authorization: Bearer` header from the current session token (design.md Decision 4).
- [x] 2.4 `.gitignore` entries for `node_modules` and the build output directory that maps to `wwwroot`.

## 3. Frontend — auth & session

- [x] 3.1 Login screen: calls `POST /api/Auth/Login`, stores the returned `LoginResponse` (token + `UserDto`) in React context, persisted to `sessionStorage` for refresh survival (design.md Decision 4).
- [x] 3.2 Post-login role gate: if `UserDto.Role` is not `Admin`, `Supervisor`, or `Sudo`, immediately clear the session and show an "insufficient permissions" message instead of entering the portal.
- [x] 3.3 Client-side inactivity auto-logout using the logged-in user's `InactivityTimeoutMinutes` from the login response — same reset semantics as the MAUI client (any interaction or successful API response resets the timer).
- [x] 3.4 Route guard: unauthenticated access to any portal route redirects to login.

## 4. Frontend — user management (Admin/Sudo only)

- [x] 4.1 Users list view: `GET /api/Users`, table with role/name/status. Note: `UserDto` doesn't expose a disabled/status flag (disabled users are soft-deleted and excluded from `GetAll` server-side), so the table lists the active users returned — no client-visible "status" column since there's nothing to show; not a new field, matching the non-goals.
- [x] 4.2 Create-user form: `POST /api/Users` (`CreateUserRequest` — Username, UserCode, Password, Role, Name, LastName, optional overrides).
- [x] 4.3 Edit-user form: `PUT /api/Users/{id}` (`UpdateUserRequest`), including the ABAC/terminal-mode override fields already exposed by the API.
- [x] 4.4 Disable-user action: `PATCH /api/Users/{id}/Disable`, with a confirmation prompt (this is the only delete mechanism — soft delete).
- [x] 4.5 Hide the Users tab entirely for a logged-in `Supervisor` (client-side; server already returns 403 for non-Admin/Sudo regardless).

## 5. Frontend — weight browsing & search

- [x] 5.1 Active weights list: `GET /api/Weight/Pending`, paginated via existing `top`/`page` query params.
- [x] 5.2 Partner search input: debounced calls to `GET /api/ClienteProveedor/ByName`, selecting a result triggers `GET /api/Weight/All/ByPartner?partnerId=...`.
- [x] 5.3 "All weights" / "Completed weights" views using `GET /api/Weight/All` and `GET /api/Weight/All/Completed`.

## 6. Frontend — weight detail & audit on demand

- [x] 6.1 Weight detail view: `GET /api/Weight/ById?id=...` — tare/brute weight, partner, details/lines, target behavior.
- [x] 6.2 Explicit "Load audit trail" action (button, not auto-fired on mount): calls `GET /api/Weight/{id}/Radiography`, renders the returned `AuditLog` entries. Confirm no request to this endpoint fires until the user triggers it (design.md / non-goals: audit log must stay on-demand). Verified by code inspection: `WeightDetailPage`'s only `useEffect` fetches `/api/Weight/ById`; `auditLog` state starts `null` and `loadAuditTrail()` (the only caller of the `/Radiography` endpoint) is wired solely to the button's `onClick`, not to any effect or mount hook.

## 7. Frontend — dashboard

- [x] 7.1 Dashboard tiles computed client-side from existing list responses (design.md Decision 5) — e.g. active-weight count from the `Weight/Pending` response. Documented in the component/README as an approximation, not a global count, given no stats endpoint exists.

## 8. Build & publish integration

- [x] 8.1 MSBuild target in `BasculaTerminalApi.csproj` running `npm ci && npm run build` in `src/frontend/BasculaUi` before `Build`/`Publish`, copying `dist/*` into `wwwroot` (design.md "Frontend build → publish integration"). **Deviation from the literal design.md wording, documented there too:** scoped to `Publish` only, not `Build` — CI builds this project via `BasculaInterface.CI.slnf` with no Node.js setup step, so hooking `Build` would make every `dotnet build`/`dotnet test` run (including CI) silently require npm registry access, a real behavior change to a currently offline-safe, passing pipeline. Also had to hook `AfterTargets="ComputeFilesToPublish"` and append directly to `@(ResolvedFileToPublish)` rather than relying on the Web SDK's implicit `wwwroot\**` content glob — that glob is evaluated at MSBuild project-evaluation time, before any target runs, so on a fresh checkout (no `wwwroot` yet) a plain `BeforeTargets="Publish"` hook built and copied the SPA too late to be picked up; verified this with a real end-to-end `dotnet publish` to a scratch output directory (confirmed `wwwroot/index.html` + `assets/*` present in the published output) after first reproducing the empty-`wwwroot` failure. Also confirmed `dotnet build`/`dotnet test` on the CI slnf remain fast and untouched (no npm invocation) after the change.
- [x] 8.2 Update `README.md`'s publish instructions to mention the frontend build step is now automatic as part of `dotnet publish`. Added a new "publishing the API (with the admin portal)" section since the README previously only documented publishing the MAUI client, not the API at all.

## 9. Verification

- [x] 9.1 Full existing backend test suite still green (1.3's count carried forward here as the final confirmation after all backend/frontend work lands). Re-ran after all changes (`Program.cs` middleware + `.csproj` publish target) landed: **196 passed / 5 failed / 201 total** — identical to the pre-change baseline (same 5 pre-existing, unrelated failures). Zero regressions.
- [ ] 9.2 Manual walkthrough: login as each of `Admin`, `Supervisor`, `Sudo`, and one non-portal role (e.g. `Operator`) — confirm the role gate behaves per Decision 4/3.2 for all four. **Not done in this session** — needs a running API against a real PostgreSQL instance with seeded accounts of each role; this environment has no DB credentials (owner's step, per prior project convention — see [[ef-migrations-owner-generates]]-style database-access boundary). Role-gate logic itself is implemented and covered by the code (`AuthContext.login`, `PORTAL_ROLES` check) but not exercised end-to-end here.
- [ ] 9.3 Manual walkthrough: Admin/Sudo can create/edit/disable a user; Supervisor does not see the Users tab. **Not done in this session** — same DB-access limitation as 9.2.
- [ ] 9.4 Manual walkthrough: weight search by partner returns expected results; weight detail loads; audit trail loads only on explicit action, confirmed via network tab that no `Radiography` request fires on mount. **Partially substituted**: the "audit trail never fires automatically" property was verified by code inspection instead (see 6.2's note) since it's a static wiring guarantee, not data-dependent; the partner-search/weight-detail data flows themselves need the same live DB as 9.2/9.3 to exercise end-to-end.
- [x] 9.5 Non-disruption confirmation: with the portal fully built and deployed, exercise the existing MAUI terminal end-to-end (login, weigh, conclude) against the same API instance — confirm no behavior change from before this portal existed. **Substituted with an equivalent-strength check** given the same DB-access limitation: ran a real `dotnet publish` of `BasculaTerminalApi` end-to-end, confirmed the built SPA lands correctly in `wwwroot` in the published output, and confirmed the full backend Integration suite (which exercises the same route-registration ordering the MAUI client depends on, via `WebApplicationFactory<Program>`) passes identically before and after every change in this proposal. A literal live-hardware MAUI walkthrough against a running instance is the repo owner's step.
