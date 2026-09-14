# Authorization Policy — Spec

## Purpose

Defines the API's default authorization posture introduced alongside user authentication (issue #134): every mutating endpoint requires an authenticated user by default, while every pre-existing read endpoint and the bascula websocket remain open.

## Requirements

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

### Requirement: The single-scale device lock remains fully unauthenticated
**Bug fix, 2026-09-13** (role-driven-terminal-modes): `WeightController.RequestWeight` (`PUT /api/Weight/CanWeight`) and `ReleaseWeight` (`PUT /api/Weight/ReleaseWeight`) SHALL carry `[AllowAnonymous]`. These are pure device-coordination primitives keyed by `deviceId` — which physical terminal currently holds the single scale — with no association to any user or record, the same in spirit as the bascula websocket. They never carried this attribute when the fallback-authenticated policy was originally introduced; any authentication hiccup (e.g. the very first request right after a fresh login, racing the client's token-attaching handler) surfaced as a misleading "bascula ocupada" instead of the real `401`.

#### Scenario: Requesting or releasing the scale lock requires no login
- **WHEN** a client calls `PUT /api/Weight/CanWeight?deviceId=...` or `PUT /api/Weight/ReleaseWeight?deviceId=...` without an `Authorization` header
- **THEN** the server processes the request normally, based solely on the device lock's own state — never rejecting it for lack of authentication
