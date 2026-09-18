# Self-Authorize Gate — Spec

## Purpose

Defines the replacement of the old shared, unsalted `PasswordHash` gate (used by `WeightEntry`/`WeightDetail`/`Pedido`/`PedidoLine` guarded mutations) with a per-user `GateIdentifier`/`GatePassword` credential checked against the real `User` table and the `CanSelfAuthorizeGate` permission.

## Requirements

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

### Requirement: The self-authorize-gate turn-bypass is a per-use gate, not a standing device permission
The device-local `BypasTurn` setting SHALL remain a per-device `Preferences` value, but SHALL NOT silently skip the single-scale busy check on its own. Actually bypassing the check at weigh-time SHALL require a valid self-authorize-gate credential (`GateIdentifier`/`GatePassword`), verified the same way as every other gated action, via `POST /api/Weight/AuthorizeTurnBypass`.

#### Scenario: BypasTurn enabled still requires a gate credential to actually bypass
- **WHEN** a terminal with `BypasTurn` enabled attempts to weigh while the scale-lock reports busy
- **THEN** the client prompts for a `GateIdentifier`/`GatePassword` credential before proceeding, and the bypass is only granted if that credential resolves to a user with `CanSelfAuthorizeGate` (or `Sudo`)

#### Scenario: An invalid credential does not bypass the lock
- **WHEN** the credential presented fails to resolve, fails password verification, or lacks the permission
- **THEN** the scale-lock busy state is enforced exactly as if `BypasTurn` were disabled
