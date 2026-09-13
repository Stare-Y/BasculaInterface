## ADDED Requirements

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
The client SHALL track user inactivity and, after a configurable threshold (default 10 minutes), clear the stored token and session and return to the login screen. No server-side session or last-activity state SHALL be required for this behavior.

#### Scenario: Inactivity beyond the threshold logs the user out
- **WHEN** no user input is registered for the configured inactivity duration
- **THEN** the client discards its session and shows the login screen

#### Scenario: Activity resets the inactivity timer
- **WHEN** the user interacts with the app (touch input, or a successful API call) before the threshold elapses
- **THEN** the inactivity timer resets and no logout occurs

### Requirement: Manual-weight capture is gated by the logged-in user's permission, not a device setting
The device-local "Capturar peso manualmente" toggle SHALL be removed. Whether manual weight entry is available SHALL be determined solely by the logged-in user's effective `CanCaptureWeightManually` permission.

#### Scenario: A user without the permission cannot capture weight manually
- **WHEN** the logged-in user's effective `CanCaptureWeightManually` is `false`
- **THEN** the manual-weight-entry option is unavailable, regardless of any prior device setting

#### Scenario: A user with the permission can capture weight manually
- **WHEN** the logged-in user's effective `CanCaptureWeightManually` is `true`
- **THEN** the manual-weight-entry option is available
