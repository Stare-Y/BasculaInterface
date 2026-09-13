## ADDED Requirements

### Requirement: Gated actions accept an identifier and plaintext password
Each of the following actions SHALL accept a `GateIdentifier` (string) and `GatePassword` (plaintext string) in place of the previous `PasswordHash` field: `WeightEntry` delete, `WeightDetail` delete, change detail product, change partner, change detail amount, `Pedido` delete, `PedidoLine` delete.

#### Scenario: Gate credential replaces the old shared password field
- **WHEN** a client calls any of the seven gated actions
- **THEN** the request body carries `GateIdentifier`/`GatePassword` instead of `PasswordHash`

### Requirement: Gate authorization resolves the identifier and checks the permission flag
The server SHALL resolve `GateIdentifier` using the same `UserCode`-then-`Username` order as login, verify `GatePassword` against that user's stored salted hash, and require the resolved user's effective `CanSelfAuthorizeGate` to be `true` (or the user's role to be `Sudo`).

#### Scenario: A different user than the one logged in can authorize
- **WHEN** the credential presented at the gate belongs to a user other than whoever is currently logged into the terminal, and that user has `CanSelfAuthorizeGate` effective `true`
- **THEN** the gated action proceeds

#### Scenario: A resolved user without the permission is rejected
- **WHEN** `GateIdentifier`/`GatePassword` resolve to a valid user whose effective `CanSelfAuthorizeGate` is `false`
- **THEN** the gated action is rejected with the same error behavior as an incorrect password today

#### Scenario: An unresolvable identifier is rejected
- **WHEN** `GateIdentifier` matches no `UserCode` or `Username`
- **THEN** the gated action is rejected

### Requirement: The shared password mechanism is removed
`WeightSettings.ChangeProductPasswordHash` and all inline comparisons against it SHALL be deleted. No gated action SHALL compare against a shared, non-user-specific secret.

#### Scenario: Old shared-password requests no longer authorize anything
- **WHEN** a client sends a request carrying only the old `PasswordHash` field (no `GateIdentifier`/`GatePassword`)
- **THEN** the request does not satisfy the gate (the field is no longer read for authorization purposes)
