## ADDED Requirements

### Requirement: Terminal behavior is derived from the logged-in user, not a device setting
The system SHALL expose a `TerminalMode` value (`Main`, `Secondary`, `PedidosOnly`, `OnlyFinished`) resolved per-user: a role-based default, overridable per-user regardless of role, the same resolution shape as the existing `CanSelfAuthorizeGate`/`CanCaptureWeightManually` permission flags. The client SHALL NOT read terminal-mode behavior from any per-device `Preferences` key.

#### Scenario: A role's default terminal mode applies with no override
- **WHEN** a user with no `TerminalModeOverride` and role `DispatchingOperator` logs in
- **THEN** the resolved `TerminalMode` is `Secondary`

#### Scenario: An override takes precedence over the role default
- **WHEN** a user's `TerminalModeOverride` is explicitly set to `OnlyFinished`, regardless of role
- **THEN** the resolved `TerminalMode` is `OnlyFinished`

#### Scenario: The client uses the session's resolved terminal mode
- **WHEN** the client needs to decide whether to behave as the secondary-scale, pedidos-only, or finished-only terminal
- **THEN** it reads the logged-in user's resolved `TerminalMode` from the current session, not a `Preferences` key

### Requirement: Role default terminal modes
Each role SHALL have a default `TerminalMode`: `Operator`, `Supervisor`, `Admin`, and `Sudo` default to `Main`; `Dispatching Operator` defaults to `Secondary`; `Purchasing Operator` defaults to `PedidosOnly`. No role SHALL default to `OnlyFinished` — it is reachable only through a per-user override.

#### Scenario: Dispatching Operator defaults to the secondary terminal
- **WHEN** a `Dispatching Operator` user with no override is evaluated
- **THEN** the resolved `TerminalMode` is `Secondary`

#### Scenario: Purchasing Operator defaults to pedidos-only
- **WHEN** a `Purchasing Operator` user with no override is evaluated
- **THEN** the resolved `TerminalMode` is `PedidosOnly`

#### Scenario: Sudo is not special-cased for terminal mode
- **WHEN** a `Sudo` user with no override is evaluated for `TerminalMode`
- **THEN** the resolved value is `Main`, exactly like `Operator`/`Supervisor`/`Admin` — `Sudo`'s authorization bypass does not extend to terminal-mode resolution

### Requirement: The self-authorize-gate turn-bypass is a per-use gate, not a standing device permission
The device-local `BypasTurn` setting SHALL remain a per-device `Preferences` value, but SHALL NOT silently skip the single-scale busy check on its own. Actually bypassing the check at weigh-time SHALL require a valid self-authorize-gate credential (`GateIdentifier`/`GatePassword`), verified the same way as every other gated action.

#### Scenario: BypasTurn enabled still requires a gate credential to actually bypass
- **WHEN** a terminal with `BypasTurn` enabled attempts to weigh while the scale-lock reports busy
- **THEN** the client prompts for a `GateIdentifier`/`GatePassword` credential before proceeding, and the bypass is only granted if that credential resolves to a user with `CanSelfAuthorizeGate` (or `Sudo`)

#### Scenario: An invalid credential does not bypass the lock
- **WHEN** the credential presented fails to resolve, fails password verification, or lacks the permission
- **THEN** the scale-lock busy state is enforced exactly as if `BypasTurn` were disabled
