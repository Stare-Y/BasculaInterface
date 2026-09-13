# Session Management — Spec

## Purpose

Defines how the MAUI terminal (`BasculaInterface`) logs a user in, stores and attaches the session token, and enforces a client-side inactivity logout — including the per-role-defaulted, per-user configurable timeout duration and the two production bug fixes (a missing global activity hook, and a timeout handler that only unwound part of the navigation stack) applied after the original mechanism shipped incomplete.

## Requirements

### Requirement: MAUI terminal presents a real login form
`MainPage` SHALL present a form collecting an identifier and password and submit them to `POST /api/Auth/Login`, replacing the previous press-and-hold gesture that collected no credential.

#### Scenario: Successful login navigates into the app
- **WHEN** an operator submits a valid identifier and password
- **THEN** the client stores the returned token and session, and navigates to the pending-weights (or finished-weights, per existing preference) screen

#### Scenario: Failed login shows an error and does not navigate
- **WHEN** an operator submits an invalid identifier or password
- **THEN** an error is shown and no navigation occurs

### Requirement: Token is stored securely and attached to every request
The client SHALL store the JWT in `SecureStorage` (not `Preferences`) and attach it as an `Authorization: Bearer` header to every API request via a shared HTTP handler.

#### Scenario: Authenticated requests carry the token automatically
- **WHEN** any API call is made after a successful login
- **THEN** the request includes the current session's JWT without each call site setting it individually

### Requirement: Client-enforced inactivity logout
The client SHALL track user inactivity across the whole app — not only at login — and, after the logged-in user's configured threshold, clear the stored token and session and return to the login screen regardless of which navigation stack (modal or regular) the operator's current screen is on. No server-side session or last-activity state SHALL be required for this behavior.

#### Scenario: Inactivity beyond the threshold logs the user out from any screen
- **WHEN** no user input or successful API response is registered for the logged-in user's configured inactivity duration, while the operator is on any screen (modal-stack or regular-stack)
- **THEN** the client discards its session and returns to the login screen, dismissing whatever screens were open

#### Scenario: A concurrent user-triggered navigation is not disrupted
- **WHEN** the inactivity timeout elapses at the same moment a user-triggered navigation (e.g. a button's own push/pop) is already in progress
- **THEN** the timeout does not interrupt that navigation; it is safe to skip that cycle since the in-flight activity will itself reset the timer

### Requirement: Activity resets the inactivity timer
The client SHALL reset the inactivity countdown on any user input anywhere in the app (not only on the login screen) and on every successful API response.

#### Scenario: A tap anywhere in the app resets the inactivity timer
- **WHEN** the user taps or touches any part of the app, on any screen
- **THEN** the inactivity timer resets and no logout occurs while such activity continues

#### Scenario: A successful API response resets the inactivity timer
- **WHEN** any API request completes with a successful status code
- **THEN** the inactivity timer resets, independent of explicit user input

### Requirement: Inactivity timeout duration is per-user and role-defaulted
Each `User` SHALL carry a non-nullable inactivity timeout duration in minutes. When a user is created through the application without an explicit value, it SHALL default based on the user's role. Any user row that did not go through that creation path SHALL still resolve to a safe, non-null default via a database-level default value.

#### Scenario: A user created through the app gets its role's default
- **WHEN** a new user is created via the user-management API without an explicit inactivity timeout
- **THEN** the stored value is `2` minutes for `Sudo`, `5` for `Admin`, `5` for `Supervisor`, `10` for `Operator`, or `20` for `DispatchingOperator`

#### Scenario: An explicit value overrides the role default
- **WHEN** a user is created or updated with an explicit inactivity timeout value
- **THEN** that value is stored regardless of the role default

#### Scenario: A row inserted outside the application still resolves to a safe default
- **WHEN** a user row is inserted directly against the database without specifying the inactivity timeout column
- **THEN** the column resolves to `5` minutes via its database-level default, never `null`

#### Scenario: The client uses the logged-in user's configured value
- **WHEN** a user logs in
- **THEN** the client starts the inactivity watcher using that user's `InactivityTimeoutMinutes` from the login response, not a hardcoded constant

### Requirement: Manual-weight capture is gated by the logged-in user's permission, not a device setting
The device-local "Capturar peso manualmente" toggle SHALL be removed. Whether manual weight entry is available SHALL be determined solely by the logged-in user's effective `CanCaptureWeightManually` permission.

#### Scenario: A user without the permission cannot capture weight manually
- **WHEN** the logged-in user's effective `CanCaptureWeightManually` is `false`
- **THEN** the manual-weight-entry option is unavailable, regardless of any prior device setting

#### Scenario: A user with the permission can capture weight manually
- **WHEN** the logged-in user's effective `CanCaptureWeightManually` is `true`
- **THEN** the manual-weight-entry option is available
