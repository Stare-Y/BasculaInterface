## ADDED Requirements

### Requirement: Backend test project layered by category
The system SHALL keep a single backend test project (`src/backend/BasculaTerminalTest`, xUnit) organised into `Unit/`, `Integration/`, `Live/`, and `TestDoubles/` folders, referencing `Core.Domain`, `Core.Application`, `Infrastructure`, and `BasculaTerminalApi`. Mocking SHALL use NSubstitute; assertions SHALL use xUnit's built-in `Assert` (no FluentAssertions).

#### Scenario: A plain checkout runs green with no infrastructure
- **WHEN** a developer runs `dotnet test --filter "Category!=Live&Category!=Integration"` on a fresh clone with no Docker
- **THEN** only the isolated unit tests run and they all pass

#### Scenario: Unit tests cover the high-risk business logic
- **WHEN** the unit suite runs
- **THEN** it exercises every branch of `PedidoService.ConvertLineToWeightAsync`, the `WeightService` manager-password gate on all four gated actions, `WeightLogisticService` turn-taking, and `PasswordHasher`

### Requirement: xUnit category traits gate execution context
Tests that need a running API plus real scale hardware SHALL be tagged `[Trait("Category","Live")]`; tests that need a Docker-compatible container engine SHALL be tagged `[Trait("Category","Integration")]`; all other tests SHALL be untagged and runnable anywhere. `dotnet test --filter "Category!=Live"` SHALL be the canonical CI command.

#### Scenario: The live hardware test never runs automatically
- **WHEN** CI or a normal `dotnet test --filter "Category!=Live"` runs
- **THEN** `BasculaClient` (the SignalR-against-real-hardware smoke test) is excluded and no test requires a serial device

#### Scenario: The live test is correct when a developer does run it
- **WHEN** a developer on the terminal PC runs `dotnet test --filter "Category=Live"` with the API up and a scale attached
- **THEN** `BasculaClient` subscribes to the hub's `"ReceiveLecture"` message (matching `SerialPortHub`) and asserts on real readings

### Requirement: API-integration tests run the real host against a throwaway database
Integration tests SHALL boot the real `BasculaTerminalApi` in-memory via `WebApplicationFactory<Program>` against a disposable PostgreSQL container (Testcontainers), letting the app's startup `db.Database.Migrate()` apply the real migrations. `IBasculaService` SHALL be replaced with a fake that raises synthetic readings; the `ContpaqiSQLContext`-backed repositories SHALL be replaced with fakes so that read-only ERP context never connects. `BasculaTerminalApi/Program.cs` SHALL expose a `public partial class Program` entry-point marker.

#### Scenario: Partial fulfilment is verified end to end
- **WHEN** an integration test creates a pedido, converts part of a line to a weight entry, and records a weight through the HTTP + SignalR + EF stack
- **THEN** re-reading the pedido shows the line's `ReceivedAmount` increased and `PendingAmount` decreased by the recorded amount, and the line is not concluded

#### Scenario: The password gate is verified at the HTTP boundary
- **WHEN** an integration test PATCHes a weight-detail amount with a wrong password hash
- **THEN** the response is `400` with body containing "Contraseña incorrecta." and the detail is unchanged; with the configured hash the same call returns `200`

#### Scenario: Ryuk-free container teardown
- **WHEN** the integration suite runs under rootless Podman
- **THEN** the Testcontainers reaper is disabled at module-init and the test fixture tears the container down itself

### Requirement: CI builds and tests the Linux-buildable projects on every push and PR
The repo SHALL contain `src/backend/BasculaInterface.CI.slnf` listing only the projects that build on Linux (`Core.Domain`, `Core.Application`, `Infrastructure`, `BasculaTerminalApi`, `BasculaTerminalTest`), and `.github/workflows/backend-tests.yml` SHALL, on push to `master` and on every pull request, restore/build that filter and run `dotnet test --filter "Category!=Live"` with a `trx` logger, surfacing results as a PR check.

#### Scenario: A PR that breaks a tested path fails CI
- **WHEN** a pull request changes `PedidoService` or `WeightService` such that a unit or integration test no longer passes
- **THEN** the `backend-tests` workflow fails and the failing tests are shown on the PR

#### Scenario: The MAUI app and the bot project are not built in CI
- **WHEN** `backend-tests.yml` runs on ubuntu-latest
- **THEN** it operates on `BasculaInterface.CI.slnf` only, never attempting to build `BasculaInterface` (MAUI) or `BasculaBotTests` (Windows-only)

### Requirement: Windows-VM bot harness drives the published app
The repo SHALL contain `src/backend/BasculaBotTests` (FlaUI, `net8.0-windows`, in the `.sln` but excluded from `BasculaInterface.CI.slnf`) and `scripts/vm/run-bot-suite.ps1` (+ a `.cmd` wrapper). The script SHALL `dotnet publish` `BasculaInterface` as an unpackaged, self-contained `WinExe`, point `BASCULA_APP_EXE` at the result, and run the bot suite, writing a `.trx` and per-window screenshots to an artifacts folder. `BasculaInterface.csproj` SHALL set `OutputType` to `WinExe`.

#### Scenario: The bot can launch and read the real app on the VM
- **WHEN** `scripts\vm\run-bot-suite.cmd` runs on `win10-maui-dev`
- **THEN** the app is published unpackaged, FlaUI launches it, the app stays running, and the smoke test reads the login screen's control tree (including the `Login` button)

#### Scenario: A failed launch still produces diagnostics
- **WHEN** the app exits during startup or never shows readable content
- **THEN** `diagnostics.txt` (process state, exit code, console output, every window's control tree) and `window-*.png` are written before the test fails

### Requirement: The build is pinned to the .NET 8 SDK
The repo root SHALL contain a `global.json` pinning the .NET 8 SDK (`rollForward: latestMinor`) so that a machine with a newer SDK (e.g. .NET 10) still builds the solution against .NET 8, matching CI.

#### Scenario: A newer SDK does not change the build
- **WHEN** the solution is built on a machine where the default `dotnet` is .NET 10
- **THEN** `global.json` selects an installed 8.0.x SDK and the build behaves as it does in CI
