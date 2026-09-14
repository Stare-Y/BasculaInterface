## MODIFIED Requirements

### Requirement: Successful login issues a JWT
On successful authentication, the server SHALL issue a JWT containing the user's id, username, usercode, and role, with a configurable expiry (default 12 hours), along with the user's effective permission flags and effective `TerminalMode` in the response body.

#### Scenario: Login response includes token and permissions
- **WHEN** a user authenticates successfully
- **THEN** the response includes a JWT and the user's effective `CanSelfAuthorizeGate`/`CanCaptureWeightManually` values

#### Scenario: Login response includes the resolved terminal mode
- **WHEN** a user authenticates successfully
- **THEN** the response body's `TerminalMode` field equals that user's `IPermissionService.GetEffectiveTerminalMode` result — never the enum's unset default — regardless of role or override
