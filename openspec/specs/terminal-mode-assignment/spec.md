# Terminal Mode Assignment — Spec

## Purpose

Defines `TerminalMode`, the per-user resolved value that determines which behavior a MAUI terminal shows once a user is logged in (issue #135 / `role-driven-terminal-modes`) — replacing the old device-local `SecondaryTerminal`/`OnlyPedidos`/`OnlyFinished` `Preferences` toggles, which had no relationship to who was actually using the terminal.

## Requirements

### Requirement: Terminal behavior is derived from the logged-in user, not a device setting
The system SHALL expose a `TerminalMode` value (`Main`, `Secondary`, `PedidosOnly`, `OnlyFinished`) resolved per-user: a role-based default, overridable per-user regardless of role, the same resolution shape as the existing `CanSelfAuthorizeGate`/`CanCaptureWeightManually` permission flags. The client SHALL NOT read terminal-mode behavior from any per-device `Preferences` key. The per-user override exists for one-off terminal exceptions; a role's default SHALL be the primary mechanism used in production, not a substitute for assigning the correct role.

#### Scenario: A role's default terminal mode applies with no override
- **WHEN** a user with no `TerminalModeOverride` and role `Dispatching Operator` logs in
- **THEN** the resolved `TerminalMode` is `Secondary`

#### Scenario: An override takes precedence over the role default
- **WHEN** a user's `TerminalModeOverride` is explicitly set to `OnlyFinished`, regardless of role
- **THEN** the resolved `TerminalMode` is `OnlyFinished`

#### Scenario: The client uses the session's resolved terminal mode
- **WHEN** the client needs to decide whether to behave as the secondary-scale, pedidos-only, or finished-only terminal
- **THEN** it reads the logged-in user's resolved `TerminalMode` from the current session, not a `Preferences` key

### Requirement: Role default terminal modes
Each role SHALL have a default `TerminalMode`: `Operator`, `Supervisor`, `Admin`, and `Sudo` default to `Main`; `Dispatching Operator` defaults to `Secondary`; `Customer Service` defaults to `PedidosOnly`. No role SHALL default to `OnlyFinished` — it is reachable only through a per-user override.

#### Scenario: Dispatching Operator defaults to the secondary terminal
- **WHEN** a `Dispatching Operator` user with no override is evaluated
- **THEN** the resolved `TerminalMode` is `Secondary`

#### Scenario: Customer Service defaults to pedidos-only
- **WHEN** a `Customer Service` user with no override is evaluated
- **THEN** the resolved `TerminalMode` is `PedidosOnly`

#### Scenario: Sudo is not special-cased for terminal mode
- **WHEN** a `Sudo` user with no override is evaluated for `TerminalMode`
- **THEN** the resolved value is `Main`, exactly like `Operator`/`Supervisor`/`Admin` — `Sudo`'s authorization bypass does not extend to terminal-mode resolution

#### Scenario: Operator and above default to the unrestricted main terminal
- **WHEN** a user with role `Operator`, `Supervisor`, `Admin`, or `Sudo` and no override is evaluated
- **THEN** the resolved `TerminalMode` is `Main`, giving access to the full flow — including pedido/purchase-order movements — the same unrestricted terminal every role other than `Customer Service` and `Dispatching Operator` receives
