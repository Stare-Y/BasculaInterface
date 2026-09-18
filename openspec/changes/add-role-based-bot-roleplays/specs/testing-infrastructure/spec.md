# Spec Delta

## ADDED Requirements

### Requirement: Role-based roleplays exercise real user flows
The bot suite SHALL include one roleplay per defined role (`Operator`, `Dispatching Operator`, `Supervisor`, `Admin`, `Sudo`, `Customer Service`) that logs in as that role's test user and exercises its confirmed flow: login and landing on the role's resolved `TerminalMode` screen; the cross-terminal weight-entry lifecycle steps applicable to that role, using direct product/partner selection (not pedido-line conversion, which is out of scope for this batch); and permission-gated actions (self-authorize gate, manual weight capture) consistent with the role's default permission flags.

#### Scenario: Each role lands on its resolved terminal-mode screen
- **WHEN** a roleplay logs in as its role's test user
- **THEN** it lands on `PendingWeightsView` with the button set matching that role's resolved `TerminalMode` default (`Main`, `Secondary`, or `PedidosOnly` — `OnlyFinished` is reachable only through a per-user override, and this batch's templates use pure role defaults with no overrides, so it is not exercised as a login landing here)

#### Scenario: FinishedWeights is reached by navigation, not login landing
- **WHEN** a Main-mode roleplay taps `BtnFinished` ("Ver Finalizados") from `PendingWeightsView`
- **THEN** it navigates to `FinishedWeights` and the screen's collection view is populated

#### Scenario: A Main-mode role births a new WeightEntry
- **WHEN** a Main-mode roleplay captures a vehicle plate and driver name and confirms the empty-truck weight
- **THEN** a new `WeightEntry` exists with `TareWeight` set and no partner or products yet

#### Scenario: A product is attached without a pedido
- **WHEN** a Main or Customer-Service-mode roleplay assigns a partner and adds a product directly to a `WeightEntry` via the product/partner pickers
- **THEN** the `WeightEntry` has an assigned partner and at least one `WeightDetail`, with no pedido line involved

#### Scenario: Partner and target behavior must be picked before a product can be picked
- **WHEN** a roleplay has not yet selected a partner and an `ExternalTargetBehavior`
- **THEN** the product picker is unavailable, and the roleplay SHALL select a partner (the fixed test partner supplied for this purpose — not an arbitrary search result) and any `ExternalTargetBehavior` option before attempting to pick a product

#### Scenario: A Dispatching Operator completes the two-step product capture
- **WHEN** a Dispatching Operator roleplay opens a product detail with no `SecondaryTare` captured
- **THEN** it captures the tare first, then the full weight, and the detail becomes loaded

#### Scenario: A Main-mode role concludes the entry
- **WHEN** all of a `WeightEntry`'s details are loaded and a Main-mode roleplay confirms conclusion
- **THEN** the entry's `ConcludeDate` is set

#### Scenario: Permission-gated actions match role defaults
- **WHEN** a Supervisor (or Sudo) roleplay attempts to self-authorize the gate or capture a weight manually
- **THEN** the action succeeds; **WHEN** an Operator or Customer Service roleplay with no override attempts the same action
- **THEN** the action is rejected

### Requirement: Idempotent, template-reconciled test-user provisioning
The bot suite SHALL maintain one persistent test user per role rather than creating and deleting a user every run. On each run it SHALL check whether that role's user exists and matches its template (role, permission overrides, terminal-mode override — all templates default to pure role defaults with no overrides for this batch); if the user is missing it SHALL be created via `POST /api/Users`; if present but drifted from the template it SHALL be corrected via `PUT /api/Users/{id}`; if present and matching, no mutating call SHALL be made.

#### Scenario: A matching user triggers no mutation
- **WHEN** a role's test user already exists and matches its template
- **THEN** the suite makes no `POST`/`PUT` call for that user on that run

#### Scenario: A drifted user is corrected, not replaced
- **WHEN** a role's test user exists but its role, permission override, or terminal-mode override differs from the template
- **THEN** the suite corrects it via `PUT /api/Users/{id}`, and its `Id` is unchanged

#### Scenario: A missing user is created
- **WHEN** a role's test user does not exist
- **THEN** the suite creates it via `POST /api/Users`, authenticated as the bootstrap `Admin`/`Sudo` account

### Requirement: Bootstrap credentials are never committed
The bot suite SHALL read the bootstrap `Admin`/`Sudo` account's identifier and password from VM environment variables, never from a file committed to the repository.

#### Scenario: Missing bootstrap credentials fail fast
- **WHEN** the required bootstrap-credential environment variables are not set
- **THEN** the suite fails with a clear diagnostic before attempting any login or provisioning call, rather than silently skipping roleplays

### Requirement: Roleplay-driven screens carry automation identifiers
Every control a roleplay interacts with directly (clicks, types into, or asserts on) SHALL carry an `AutomationProperties.AutomationId`, across `MainPage`, `PendingWeightsView`, `FinishedWeights`, `WeightingScreen`, `DetailedWeightView`, `ReadOnlyDetailedWeightView`, `AuthorizeTurnBypassPopUp`, `ProductSelectView`, and `PartnerSelectView`.

#### Scenario: Roleplays locate controls by AutomationId
- **WHEN** a roleplay drives any control on an in-scope screen
- **THEN** it locates that control by `AutomationId`, not by `x:Name`, visual position, or text content
