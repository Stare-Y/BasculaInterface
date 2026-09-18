# Design: Admin Portal Frontend

## Context

Issue #141 asked for a web portal and explicitly flagged two open business questions the owner wanted to discuss first: the Admin/Supervisor hierarchy for user management, and where this would be hosted. Both were resolved via `enrich-issue-for-openspec` before this proposal:

- **Role hierarchy:** kept as the existing backend already enforces it — `Admin`/`Sudo` have full user CRUD (incl. disable), `Supervisor` has none. No backend authorization change.
- **Hosting:** served from `BasculaTerminalApi`'s own `wwwroot` as static files (not IIS), confirmed sufficient for the ~10-concurrent-user scale.

`openspec/config.yaml`'s own project context already anticipates this: *"Frontend web: React + TypeScript (BasculaUi) — admin/monitoring view"* — so the project name `BasculaUi` and its purpose were effectively pre-agreed before this issue existed.

## Goals / Non-Goals

**Goals:** a working admin portal (login, user management, weight browsing/search, on-demand audit) built on 100% existing API surface, served without disturbing the existing API in any way.

**Non-Goals:** any backend authorization change, any new backend endpoint, IIS hosting, pagination/stats endpoints, MAUI/terminal changes.

## Decisions

### Decision 1: Serve the SPA from `wwwroot`, with routing ordered to protect existing API behavior

**Chosen:** `Program.cs` gains, after the existing `app.MapControllers()`/hub mapping:
```csharp
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
```
`MapFallbackToFile` only ever matches requests that didn't already match a controller route, a static file, or the SignalR hub — since `MapControllers()`/`MapHub<SerialPortHub>()` are registered first (as they already are today), an unmatched `/api/...` request still falls through to ASP.NET's normal 404, not `index.html`. This is the load-bearing detail for the non-disruption constraint: **ordering, not a route exclusion list**, is what keeps `/api/*` untouched.

**Rejected — separate IIS site:** resolved against in the enrichment conversation; adds a second process/deployment artifact for no benefit at this scale.

**Rejected — reverse proxy in front of both:** unnecessary complexity for a single-machine, single-process deployment.

### Decision 2: If the frontend build is missing, the API still starts

**Chosen:** `UseStaticFiles()`/`MapFallbackToFile` targeting a `wwwroot` that doesn't exist is a supported no-op in ASP.NET Core (requests just 404 instead of serving the SPA) — no explicit existence check needed, but this is called out as a task-level verification item (build and run the API with `wwwroot` deleted, confirm existing endpoints still respond) since it's the concrete way the non-disruption constraint gets checked, not just asserted.

### Decision 3: Frontend stack — Vite + React + TypeScript, `src/frontend/BasculaUi`

**Chosen:** Vite for the build tool (fast dev server, simple static `dist/` output that maps directly onto `wwwroot`), `react-router-dom` for the three-tab navigation, a thin `fetch`-based API client (no heavy HTTP library needed — this API surface is small). No component library mandated; plain CSS or a lightweight one (e.g. minimal utility classes) is left to implementation, since the issue didn't specify visual requirements beyond "usual view with a side tab."

**Rejected — Next.js/SSR framework:** this is an internal, API-backed SPA with no SEO/SSR need; adds build complexity for no benefit over a plain Vite SPA that outputs static files.

### Decision 4: Auth/session handling mirrors the MAUI client's pattern, adapted for the web

The existing `session-management` spec governs the MAUI client specifically (`SecureStorage`, native inactivity watcher). The web portal needs its own equivalent, not a spec change to that MAUI-scoped spec:
- JWT held in memory (React context) for the session; persisted to `sessionStorage` only so a page refresh doesn't force a re-login mid-session — `sessionStorage` (not `localStorage`) so the token doesn't outlive the browser tab.
- Every API call attaches `Authorization: Bearer <token>` via a shared fetch wrapper, same pattern as the MAUI `HttpMessageHandler`.
- Reuse `UserDto.InactivityTimeoutMinutes` from the login response to drive a client-side inactivity auto-logout, same threshold semantics as the terminal (activity = any click/keypress or successful API response resets the timer).
- Role check after login: if `UserDto.Role` is not `Admin`, `Supervisor`, or `Sudo`, the client logs the session out immediately and shows an "insufficient permissions" message — the JWT itself isn't role-restricted server-side (only `Users*` endpoints are), so this is a client-side gate, same as the enrichment noted.

### Decision 5: Dashboard is computed client-side from existing list responses (documented gap)

**Chosen:** no backend stats endpoint exists. The dashboard's first iteration shows tiles derived from data already being fetched for other screens (e.g. active-weight count from `Weight/Pending`'s result length at whatever `top` is requested). This is explicitly an approximation — accurate global counts across the whole table aren't obtainable from current endpoints without a dedicated stats endpoint, which is a deferred non-goal.

**Rejected — add a stats endpoint now:** out of scope per the non-disruption/no-new-endpoints stance in the enrichment; revisit if/when real usage shows this is insufficient.

### Decision 6: Weight/partner search UX

Partner search field debounces input against `ClienteProveedor/ByName`, and selecting a result calls `Weight/All/ByPartner?partnerId=...`. No client-side caching layer beyond basic debounce — traffic volume at this scale doesn't justify one.

## Frontend build → publish integration

`BasculaTerminalApi.csproj` gains an MSBuild target that runs `npm ci && npm run build` in `src/frontend/BasculaUi`, copying `dist/*` into `wwwroot`. This keeps `dotnet publish` a single command for the whole deployable, consistent with the existing README's publish instructions, without requiring a separate CI pipeline right away. `wwwroot` (and `node_modules`) are gitignored — only source under `src/frontend/BasculaUi` is committed.

**Implementation refinement (found during apply, not anticipated here originally):** the target is scoped to `Publish` only, not `Build` — this project builds under CI via `BasculaInterface.CI.slnf`, which has no Node.js setup step, so a `Build`-scoped hook would make every `dotnet build`/`dotnet test` run (CI included) silently require npm registry access, changing a currently offline-safe, passing pipeline. It's also hooked as `AfterTargets="ComputeFilesToPublish"`, appending straight to `@(ResolvedFileToPublish)`, rather than relying on the Web SDK's implicit `wwwroot\**` content glob — that glob is evaluated at MSBuild project-evaluation time (before any target runs), so on a fresh checkout without a pre-existing `wwwroot`, a plain `BeforeTargets="Publish"` hook builds and copies the SPA too late for the glob to have picked it up, and `dotnet publish` would silently ship without the portal. Verified end-to-end against a real `dotnet publish` output directory.

## Open Questions

None outstanding — the two business questions this issue raised were resolved before this proposal (see Context). Implementation-level choices in this design (build tool, storage, dashboard approach) are defaults chosen to keep momentum; revisit only if they prove insufficient in practice.
