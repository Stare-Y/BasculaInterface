## MODIFIED Requirements

### Requirement: Gate authorization resolves the identifier and checks the permission flag
The server SHALL resolve `GateIdentifier` using the same `UserCode`-then-`Username` order as login, verify `GatePassword` against that user's stored salted hash, and require the resolved user's effective `CanSelfAuthorizeGate` to be `true` (or the user's role to be `Sudo`). This mechanism now also backs the turn-bypass verification endpoint (`POST /api/Weight/AuthorizeTurnBypass`), alongside the seven pre-existing mutating gated actions — unlike those, this endpoint performs no mutation itself, only verification.

#### Scenario: A different user than the one logged in can authorize
- **WHEN** the credential presented at the gate belongs to a user other than whoever is currently logged into the terminal, and that user has `CanSelfAuthorizeGate` effective `true`
- **THEN** the gated action proceeds

#### Scenario: A resolved user without the permission is rejected
- **WHEN** `GateIdentifier`/`GatePassword` resolve to a valid user whose effective `CanSelfAuthorizeGate` is `false`
- **THEN** the gated action is rejected with the same error behavior as an incorrect password today

#### Scenario: An unresolvable identifier is rejected
- **WHEN** `GateIdentifier` matches no `UserCode` or `Username`
- **THEN** the gated action is rejected

#### Scenario: The turn-bypass verification endpoint uses the same rules
- **WHEN** `POST /api/Weight/AuthorizeTurnBypass` is called with a `GateIdentifier`/`GatePassword`
- **THEN** it returns `true` only under the exact same resolution/verification/permission rules as every other gated action, and performs no mutation regardless of the result
