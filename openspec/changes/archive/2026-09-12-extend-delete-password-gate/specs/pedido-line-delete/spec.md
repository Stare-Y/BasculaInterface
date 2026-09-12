## ADDED Requirements

### Requirement: Delete a PedidoLine via a guarded endpoint
The system SHALL expose `PATCH /api/Pedido/Line/{id}/Delete` to soft-delete an existing `PedidoLine` (`IsDeleted=true`), replacing the previous unguarded `DELETE /api/Pedido/Line?id=`. This endpoint currently has no UI caller; it SHALL still be password-gated so a future caller cannot reintroduce an unguarded delete path.

#### Scenario: Successfully delete a PedidoLine
- **WHEN** a caller sends `PATCH /api/Pedido/Line/{id}/Delete` with the correct `PasswordHash`, for an existing, not-yet-deleted `PedidoLine`
- **THEN** `line.IsDeleted` is set to `true` and the server returns `200 OK`

#### Scenario: Reject deleting a line that does not exist or is already deleted
- **WHEN** a caller sends `PATCH /api/Pedido/Line/{id}/Delete` for an `id` with no matching non-deleted `PedidoLine`
- **THEN** the server returns `404 Not Found`

### Requirement: Password gate on PedidoLine deletion
The system SHALL require a `PasswordHash` in the request body, compared against the same shared password hash used elsewhere (`WeightSettings.ChangeProductPasswordHash`).

#### Scenario: Reject on incorrect password
- **WHEN** a caller sends `PATCH /api/Pedido/Line/{id}/Delete` with a `PasswordHash` that does not match the configured hash
- **THEN** the server returns `400 Bad Request` with a message indicating an incorrect password, and the line is not deleted

### Requirement: Previous unguarded route is removed
The pre-existing `DELETE /api/Pedido/Line?id={id}` endpoint SHALL no longer exist.

#### Scenario: Old route no longer resolves
- **WHEN** a client sends `DELETE /api/Pedido/Line?id={id}`
- **THEN** the server returns a 404/405 routing response, not a successful deletion
