# Pedido Line Delete — Spec

## Purpose

Defines the gated endpoint that lets a caller soft-delete a `PedidoLine`, replacing the previous unguarded `DELETE /api/Pedido/Line?id=`. This endpoint has no UI caller yet; it is gated anyway so a future caller cannot reintroduce an unguarded delete path (issue #133 / extend-delete-password-gate). Originally a single shared password, superseded by the per-user `GateIdentifier`/`GatePassword` self-authorize gate once `add-user-authentication-and-audit-log` introduced real user identity — see `self-authorize-gate`.

## Requirements

### Requirement: Delete a PedidoLine via a guarded endpoint
The system SHALL expose `PATCH /api/Pedido/Line/{id}/Delete` to soft-delete an existing `PedidoLine` (`IsDeleted=true`), replacing the previous unguarded `DELETE /api/Pedido/Line?id=`. This endpoint currently has no UI caller; it SHALL still be password-gated so a future caller cannot reintroduce an unguarded delete path.

#### Scenario: Successfully delete a PedidoLine
- **WHEN** a caller sends `PATCH /api/Pedido/Line/{id}/Delete` with a valid `GateIdentifier`/`GatePassword` credential, for an existing, not-yet-deleted `PedidoLine`
- **THEN** `line.IsDeleted` is set to `true` and the server returns `200 OK`

#### Scenario: Reject deleting a line that does not exist or is already deleted
- **WHEN** a caller sends `PATCH /api/Pedido/Line/{id}/Delete` for an `id` with no matching non-deleted `PedidoLine`
- **THEN** the server returns `404 Not Found`

### Requirement: Self-authorize gate on PedidoLine deletion
**Updated by `add-user-authentication-and-audit-log`**: the system SHALL require a `GateIdentifier`/`GatePassword` credential in the request body, resolved and verified per the `self-authorize-gate` capability.

#### Scenario: Reject an unauthorized gate credential
- **WHEN** a caller sends `PATCH /api/Pedido/Line/{id}/Delete` with a `GateIdentifier`/`GatePassword` that fails to resolve to a user, fails password verification, or resolves to a user whose effective `CanSelfAuthorizeGate` is `false`
- **THEN** the server returns `400 Bad Request` and the line is not deleted

### Requirement: Previous unguarded route is removed
The pre-existing `DELETE /api/Pedido/Line?id={id}` endpoint SHALL no longer exist.

#### Scenario: Old route no longer resolves
- **WHEN** a client sends `DELETE /api/Pedido/Line?id={id}`
- **THEN** the server returns a 404/405 routing response, not a successful deletion
