# Spec Delta

## MODIFIED Requirements

### Requirement: Terminal behavior is derived from the logged-in user, not a device setting
The system SHALL expose a `TerminalMode` value (`Main`, `Secondary`, `PedidosOnly`, `OnlyFinished`) resolved at login with the following precedence: (1) if the terminal being logged into has a configured forced role, `TerminalMode` is that forced role's default `TerminalMode`; (2) otherwise, `TerminalMode` resolves per-user exactly as before — a role-based default, overridable per-user via `TerminalModeOverride` regardless of role. The client SHALL NOT read terminal-mode behavior from any per-device `Preferences` key. The per-user override exists for one-off exceptions on terminals with no forced role configured; a role's default SHALL be the primary mechanism used in production, not a substitute for assigning the correct role.

#### Scenario: A role's default terminal mode applies with no override
- **WHEN** a user with no `TerminalModeOverride` and role `Dispatching Operator` logs in at a terminal with no configured forced role
- **THEN** the resolved `TerminalMode` is `Secondary`

#### Scenario: An override takes precedence over the role default
- **WHEN** a user's `TerminalModeOverride` is explicitly set to `OnlyFinished`, regardless of role, and the terminal they log in at has no configured forced role
- **THEN** the resolved `TerminalMode` is `OnlyFinished`

#### Scenario: A terminal's forced role takes precedence over the user's own role and override
- **WHEN** a user logs in at a terminal with a configured forced role, regardless of that user's own role or `TerminalModeOverride`
- **THEN** the resolved `TerminalMode` is that forced role's default `TerminalMode`

#### Scenario: The client uses the session's resolved terminal mode
- **WHEN** the client needs to decide whether to behave as the secondary-scale, pedidos-only, or finished-only terminal
- **THEN** it reads the logged-in user's resolved `TerminalMode` from the current session, not a `Preferences` key
