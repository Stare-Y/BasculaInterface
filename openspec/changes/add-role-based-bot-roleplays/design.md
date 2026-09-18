# Design

## Context

`BasculaBotTests` today is one test (`AppLaunchSmokeTests`) plus `AppDriver`, a thin wrapper around FlaUI/UIA3 that launches the published Windows app, finds its main window, and writes diagnostics — no click/type/find-by-id helpers exist yet. Only `PedidoFormView` carries `AutomationId`s. The suite runs exclusively on a Windows VM (or another Windows PC); `scripts/vm/run-bot-suite.ps1` publishes the app, optionally starts a local `BasculaTerminalApi` (`-StartApi`), runs the suite, and collects a `.trx` + screenshots. See `proposal.md` for the full motivation; this document covers the how.

The six roles, their default `TerminalMode` and default permission flags are fixed by `role-permission-model` and `terminal-mode-assignment` — this change does not touch those, only exercises them.

## Goals / Non-Goals

**Goals:**
- Drive the real app through login, role-derived landing, the direct-pick weight lifecycle, and permission gates, for all six roles.
- Keep test-user state idempotent and API-provisioned, never seeded.
- Leave every screen the roleplays touch locatable by stable `AutomationId`.

**Non-Goals:**
- Pedido CRUD or pedido-line-to-weight conversion (`ConvertLineToWeightPopUp` / "Pesar").
- The zero-tare "new entry from Main" distare workaround.
- CI integration or Linux execution — VM/Windows-only, unchanged.
- Designing the bootstrap Admin/Sudo account's own provisioning — assumed to pre-exist.

## Decisions

### Test-user templates (six fixed rows, pure role defaults)

| UserCode | Role | Permission overrides | TerminalMode override |
|---|---|---|---|
| `BOTOPERATOR` | Operator | none (inherit) | none |
| `BOTDISPATCH` | Dispatching Operator | none | none |
| `BOTSUPERVISOR` | Supervisor | none | none |
| `BOTADMIN` | Admin | none | none |
| `BOTSUDO` | Sudo | none | none |
| `BOTCUSTSVC` | Customer Service | none | none |

No override case is included in this batch (deferred — see proposal Non-goals precedent). A fixed, generated-but-stable password per user is stored the same way as the bootstrap credentials (VM environment variables), since `POST /api/Users`/`PUT` require sending a plaintext password and the suite must be able to log in as each of them.

**Provisioning algorithm** (runs once per test session, before any roleplay):
1. Log in as the bootstrap Admin/Sudo account (identifier + password from env vars).
2. `GET /api/Users`, filter client-side for each of the six `UserCode`s.
3. For each missing row: `POST /api/Users` with its template.
4. For each present row whose `Role`/override fields differ from its template: `PUT /api/Users/{id}` to correct it.
5. For each present row already matching: no call.

Rejected alternative: create-then-delete per run (matches the existing pedido/weight-entry "tag and clean up" convention) — rejected because provisioning six users through the Admin-only API on every run adds latency and log noise for no benefit; users have no meaningful "residue" the way test pedidos/weight entries do.

### Fixed test partner

The weight-lifecycle roleplays need one real partner to select in `PartnerSelectView`. Per the project owner: search **"Claro Cervantes"** — confirmed to be the only match returned, so the roleplay can safely select the single search result without disambiguating further. This is a fixed dependency on that partner continuing to exist (and remain uniquely matched) in the target dev database; it is not created or managed by the suite.

### Product-attachment ordering constraint

The product picker (`BtnPickProduct` / `ProductSelectView`) is unusable until both a partner and an `ExternalTargetBehavior` are selected on the same screen. Roleplays MUST select partner (the fixed "Claro Cervantes") and `ExternalTargetBehavior` (any picker option — no specific value matters for the test) before attempting to open the product picker, or the picker action will not be available to drive.

### AppDriver gets a minimal interaction layer, not a full page-object framework

Add to `AppDriver` (or a new small `AppElements`/`Screen` helper alongside it): `FindByAutomationId(window, id)`, `Click(element)`, `TypeText(element, text)`, `WaitForElement(window, id, timeout)`. Each roleplay test class still owns its own step sequence directly (no per-screen page-object classes yet) — six-plus roleplays is not yet enough repetition to justify that abstraction; revisit if a follow-up change adds many more.

### Bootstrap and per-role credentials live in VM environment variables

New variables, following the existing `PostgresWeightConnection`/`ContpaqSQLConnection` naming convention:
- `BasculaBotAdminIdentifier` / `BasculaBotAdminPassword` — the pre-existing bootstrap Admin/Sudo account.
- `BasculaBotRolePassword` — a single shared password used for all six `BOT*` template users (simpler than six separate variables; these are non-production, suite-only accounts whose only access is what their role/permissions grant).

`run-bot-suite.ps1` reads these and fails fast (per the spec's "Missing bootstrap credentials fail fast" scenario) if the two bootstrap variables are absent. The suite runs with a live API by default for this batch (roleplays need one) — `-StartApi` becomes the default path rather than opt-in; a `-NoApi` switch can be added if a future scenario needs the launch-only mode preserved.

### Weight-lifecycle roleplay session shape

Open question below — see **Open Questions**.

## Risks / Trade-offs

- **Shared role password** (`BasculaBotRolePassword` for all six template users) → smaller blast radius than production accounts since they're role-default, no-override, suite-only identities; acceptable trade-off for one fewer set of variables to manage.
- **Fixed test partner is a single point of failure**: if "Claro Cervantes" is renamed, disabled, or a second match appears in the target DB, the partner-select step breaks → mitigation: the roleplay asserts exactly one search result before selecting, so drift fails loudly (a clear assertion message) rather than silently picking the wrong partner.
- **Six users provisioned through the Admin-only API on every VM run** adds a few HTTP calls before any roleplay starts → acceptable; mitigated by the idempotent check-then-correct design (steady state is read-only).
- **`AutomationId` additions touch nine XAML files** with no behavior change, but each is a real edit to production UI files → mitigated by keeping the additions purely additive (attribute only, no layout/logic changes) and covered by the app's existing manual QA before each release.

## Migration Plan

No data migration. Deployment is additive: new test classes, new `AutomationId` attributes, updated `run-bot-suite.ps1`/README, and the six `BOT*` users appearing in the target dev DB the first time the suite runs there. Rollback is deleting the new test files/script changes; the `BOT*` users can be disabled via the existing `PATCH /api/Users/{id}/Disable` if ever unwanted.

## Open Questions

- **Weight-lifecycle roleplay session shape**: should the flow-A roleplay run as a single test session that logs in once and performs both the "Main births entry" and "assign partner/product" steps back-to-back (simplest to implement, but glosses over the real two-terminal handoff), or as two roleplay steps with separate logins matching the real Main → Customer Service handoff (more faithful, more moving parts)? Doesn't change the spec's observable scenarios either way — tasks.md will pick one during implementation and this can be revisited without a spec change.
