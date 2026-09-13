## MODIFIED Requirements

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

## ADDED Requirements

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
