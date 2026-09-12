# Pedido Delete — Spec

## Purpose

Defines the password-gated endpoint that lets an operator soft-delete an entire `Pedido` (cascading to its non-deleted `Lines`, unchanged from the pre-existing repo behavior), replacing the previous unguarded `DELETE /api/Pedido?id=`. Reuses the exact same shared password as every WeightEntry/WeightDetail guarded mutation — `PedidoService` takes on an `IOptions<WeightSettings>` dependency for this rather than introducing a separate setting (issue #133 / extend-delete-password-gate).

## Requirements

### Requirement: Delete a whole Pedido via a guarded endpoint
The system SHALL expose `PATCH /api/Pedido/{id}/Delete` to soft-delete an existing `Pedido` (`IsDeleted=true`, cascading to its `Lines` exactly as the existing repo logic already does), replacing the previous unguarded `DELETE /api/Pedido?id=`.

#### Scenario: Successfully delete a Pedido
- **WHEN** a terminal sends `PATCH /api/Pedido/{id}/Delete` with the correct `PasswordHash`, for an existing, not-yet-deleted `Pedido`
- **THEN** the pedido and its non-deleted lines are marked `IsDeleted=true` and the server returns `200 OK`

#### Scenario: Reject deleting a pedido that does not exist or is already deleted
- **WHEN** a terminal sends `PATCH /api/Pedido/{id}/Delete` for an `id` with no matching non-deleted `Pedido`
- **THEN** the server returns `404 Not Found`

### Requirement: Password gate on Pedido deletion
The system SHALL require a `PasswordHash` in the request body, compared against the same shared password hash used for the weight-side guarded mutations (`WeightSettings.ChangeProductPasswordHash`). The client SHALL hash the operator-entered plaintext password before sending it.

#### Scenario: Reject on incorrect password
- **WHEN** a terminal sends `PATCH /api/Pedido/{id}/Delete` with a `PasswordHash` that does not match the configured hash
- **THEN** the server returns `400 Bad Request` with a message indicating an incorrect password, and the pedido is not deleted

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
