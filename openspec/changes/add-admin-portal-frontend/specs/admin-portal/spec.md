## ADDED Requirements

### Requirement: Portal login is restricted to Admin, Supervisor, and Sudo
The admin portal (`BasculaUi`) SHALL authenticate via the existing `POST /api/Auth/Login` endpoint. After a successful login, the client SHALL check the returned user's role and SHALL clear the session and deny entry to the portal unless the role is `Admin`, `Supervisor`, or `Sudo`.

#### Scenario: An Operator cannot enter the portal
- **WHEN** a user with role `Operator`, `Dispatching Operator`, or `Customer Service` successfully authenticates via the login endpoint
- **THEN** the portal client discards the session and shows an insufficient-permissions message instead of entering the portal

#### Scenario: An Admin, Supervisor, or Sudo enters the portal
- **WHEN** a user with role `Admin`, `Supervisor`, or `Sudo` successfully authenticates
- **THEN** the portal client enters the main portal shell

### Requirement: User management is visible only to Admin and Sudo
The portal SHALL only present the user-management screen (create, edit, disable `User` accounts) to a logged-in `Admin` or `Sudo`. A logged-in `Supervisor` SHALL NOT see a user-management screen. This mirrors, and does not modify, the existing `user-administration` spec's server-side enforcement.

#### Scenario: A Supervisor does not see the Users tab
- **WHEN** a `Supervisor` is logged into the portal
- **THEN** no user-management navigation entry or screen is presented

#### Scenario: An Admin manages users through the portal
- **WHEN** an `Admin` is logged into the portal
- **THEN** they can create, edit, and disable `User` accounts through the portal, calling the existing `Users*` endpoints unchanged

### Requirement: Weight browsing and partner search
The portal SHALL present a view of active (pending) weight entries, and SHALL allow searching weight entries by partner. Selecting a weight entry SHALL show its full detail.

#### Scenario: Active weights are listed
- **WHEN** a logged-in portal user opens the weights view
- **THEN** the currently pending (not yet concluded) weight entries are listed

#### Scenario: Searching by partner filters the list
- **WHEN** a logged-in portal user selects a partner from the partner search
- **THEN** the weights list is filtered to entries for that partner

#### Scenario: Selecting a weight shows its detail
- **WHEN** a logged-in portal user selects a weight entry from any list
- **THEN** the entry's full detail (tare/brute weight, partner, details/lines, target behavior) is shown

### Requirement: The audit trail is loaded on demand, never automatically
The weight-detail view SHALL NOT request the weight entry's audit trail automatically. The audit trail SHALL only be requested when the user takes an explicit action to load it.

#### Scenario: Opening a weight's detail does not fetch its audit trail
- **WHEN** a logged-in portal user opens a weight entry's detail view
- **THEN** no request to the audit/radiography endpoint is made until the user explicitly triggers loading it

#### Scenario: Explicitly requesting the audit trail loads it
- **WHEN** a logged-in portal user triggers the "load audit trail" action on a weight's detail view
- **THEN** the portal requests and displays that entry's audit trail

### Requirement: The portal is hosted additively and does not alter existing API behavior
The portal SHALL be served as static files from `BasculaTerminalApi`'s own process, via middleware added after existing route registration so that no existing API route's behavior changes. The API SHALL start and serve all existing endpoints normally whether or not the built portal assets are present.

#### Scenario: An unmatched API route still 404s, not the portal shell
- **WHEN** a request is made to an `/api/...` path that matches no controller action
- **THEN** the server responds with its existing 404 behavior, not the portal's `index.html`

#### Scenario: The API functions with the portal assets absent
- **WHEN** the API starts without the portal's built static assets present
- **THEN** every existing endpoint continues to function exactly as before this change
