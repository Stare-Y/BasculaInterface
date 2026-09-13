## ADDED Requirements

### Requirement: Every mutating request requires an authenticated user by default
The API SHALL apply a fallback authorization policy requiring an authenticated user for any request that does not carry an explicit `[AllowAnonymous]` attribute. This SHALL apply uniformly across every controller, not be opted into per endpoint.

#### Scenario: Unauthenticated mutating request is rejected
- **WHEN** a client sends a POST/PUT/PATCH/DELETE request to any endpoint without a valid `Authorization: Bearer` token
- **THEN** the server returns `401 Unauthorized`

#### Scenario: A newly added endpoint is authenticated by default
- **WHEN** a new mutating endpoint is added to any controller without an explicit `[AllowAnonymous]` attribute
- **THEN** it requires authentication automatically, with no extra configuration

### Requirement: Every existing GET endpoint remains open
Every GET action across `WeightController`, `PedidoController`, `ProductosController`, `ClienteProveedorController`, `ExternalTargetBehaviorController`, `TurnController`, and `PrintController` (if any) SHALL carry `[AllowAnonymous]` and remain callable without authentication.

#### Scenario: Reading weight data requires no login
- **WHEN** a client calls `GET /api/Weight/Pending` (or any other existing GET endpoint) without an `Authorization` header
- **THEN** the server returns the requested data as it did before this change

### Requirement: The bascula websocket remains fully unauthenticated
`SerialPortHub` (`/basculaSocket`) SHALL carry `[AllowAnonymous]` and SHALL NOT require a token to connect, subscribe, or receive weight readings.

#### Scenario: Websocket connects without a token
- **WHEN** a client connects to `/basculaSocket` without any authentication credential
- **THEN** the connection succeeds and weight readings are delivered exactly as before this change
