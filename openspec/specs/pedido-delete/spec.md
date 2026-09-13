# Pedido Delete — Spec

## Purpose

Defines the guarded endpoint that lets an operator soft-delete an entire `Pedido` (cascading to its non-deleted `Lines`, unchanged from the pre-existing repo behavior), replacing the previous unguarded `DELETE /api/Pedido?id=` (issue #133 / extend-delete-password-gate). Originally gated by a single shared password (`WeightSettings.ChangeProductPasswordHash`); superseded by the per-user `GateIdentifier`/`GatePassword` self-authorize gate once `add-user-authentication-and-audit-log` introduced real user identity — see `self-authorize-gate`.

## Requirements

### Requirement: Delete a whole Pedido via a guarded endpoint
The system SHALL expose `PATCH /api/Pedido/{id}/Delete` to soft-delete an existing `Pedido` (`IsDeleted=true`, cascading to its `Lines` exactly as the existing repo logic already does), replacing the previous unguarded `DELETE /api/Pedido?id=`.

#### Scenario: Successfully delete a Pedido
- **WHEN** a terminal sends `PATCH /api/Pedido/{id}/Delete` with a valid `GateIdentifier`/`GatePassword` credential, for an existing, not-yet-deleted `Pedido`
- **THEN** the pedido and its non-deleted lines are marked `IsDeleted=true` and the server returns `200 OK`

#### Scenario: Reject deleting a pedido that does not exist or is already deleted
- **WHEN** a terminal sends `PATCH /api/Pedido/{id}/Delete` for an `id` with no matching non-deleted `Pedido`
- **THEN** the server returns `404 Not Found`

### Requirement: Self-authorize gate on Pedido deletion
**Updated by `add-user-authentication-and-audit-log`**: the system SHALL require a `GateIdentifier`/`GatePassword` credential in the request body, resolved and verified per the `self-authorize-gate` capability (superseding the original shared `PasswordHash`/`WeightSettings.ChangeProductPasswordHash` mechanism this requirement first shipped with).

#### Scenario: Reject an unauthorized gate credential
- **WHEN** a terminal sends `PATCH /api/Pedido/{id}/Delete` with a `GateIdentifier`/`GatePassword` that fails to resolve to a user, fails password verification, or resolves to a user whose effective `CanSelfAuthorizeGate` is `false`
- **THEN** the server returns `400 Bad Request` and the pedido is not deleted

### Requirement: Previous unguarded route is removed
The pre-existing `DELETE /api/Pedido?id={id}` endpoint SHALL no longer exist.

#### Scenario: Old route no longer resolves
- **WHEN** a client sends `DELETE /api/Pedido?id={id}`
- **THEN** the server returns a 404/405 routing response, not a successful deletion

### Requirement: MAUI client prompts for a password before deleting a Pedido
`PedidoFormView`'s delete action SHALL prompt for the manager password before calling the delete endpoint, replacing the previous plain yes/no confirmation. `PedidoListViewModel`'s delete call path SHALL carry the same password parameter, even though no View currently invokes it.

#### Scenario: Deleting a Pedido from the form requires a password
- **WHEN** an operator taps the delete button on `PedidoFormView`
- **THEN** a confirmation popup appears requesting the manager password, and no deletion occurs until a password is submitted

#### Scenario: Cancelling the password prompt aborts the deletion
- **WHEN** an operator cancels the password prompt (or submits an empty password) for deleting a Pedido
- **THEN** no request is sent and the pedido is not deleted
