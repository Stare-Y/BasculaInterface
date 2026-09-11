## Why

The backend has grown through issues #116 / #121 / #122 / #124 / #125 with no automated safety net. The only test project (`BasculaTerminalTest`) held a single test that needed a running API **and** a physical scale to execute — and it was silently broken (listened for `"ReceiveNumber"`; the hub sends `"ReceiveLecture"`). CI (`claude-code-review.yml`) only ran an AI review — no `dotnet build`, no `dotnet test`. Verification was the owner's manual smoke pass before each deploy, which does not scale as the pedido / weight-detail logic keeps compounding.

The dev environment is Linux; the MAUI client (`BasculaInterface`) only runs on Windows (the `win10-maui-dev` VM). One test approach can't cover both, so this establishes **three layers**, each runnable where it makes sense.

This change is **retroactive** — the work is implemented and verified (47 tests green; the VM bot harness proven). It exists so OpenSpec tracks the capability like the rest of the repo.

## What Changes

- **Unit layer** (`BasculaTerminalTest/Unit/`, runs anywhere incl. CI, no Docker): xUnit + NSubstitute, covering `PedidoService.ConvertLineToWeightAsync` (all branches), the `WeightService` manager-password gate (4 actions), `WeightLogisticService` turn-taking, and `PasswordHasher`.
- **API-integration layer** (`BasculaTerminalTest/Integration/`, `[Trait("Category","Integration")]`, needs Docker/Podman): the real `BasculaTerminalApi` host in-memory via `WebApplicationFactory<Program>` against a throwaway PostgreSQL (Testcontainers), with `IBasculaService` → `FakeBasculaService` and the ContpaqiSQL repos faked. Exercises pedido → convert → weigh partial-fulfillment, the HTTP password gate, and SignalR scale broadcast.
- **VM bot layer** (`src/backend/BasculaBotTests/`, Windows-only, **excluded from CI**): FlaUI drives the published MAUI Windows app; `scripts/vm/run-bot-suite.ps1` / `.cmd` publishes it unpackaged and runs the suite. A launch/read smoke test passes; concrete use-case scenarios are a follow-up change.
- **CI**: `.github/workflows/backend-tests.yml` (ubuntu-latest) builds + tests `BasculaInterface.CI.slnf` with `--filter "Category!=Live"`, publishing a report via `dorny/test-reporter`.
- **Live test**: `BasculaClient` fixed (`ReceiveNumber` → `ReceiveLecture`), tagged `[Trait("Category","Live")]`, moved to `Live/` — never run automatically.
- **Supporting fixes** (build-config / bug fixes, no runtime behaviour change): `PasswordHasher` moved `BasculaInterface` → `Core.Application/Security`; `Program.cs` gets a `public partial class Program` marker; `BasculaInterface.csproj` goes Windows-only (`net8.0-android` dropped — EOL), `OutputType` `Exe` → `WinExe` (a GUI app should never be a console exe; fixes the window flashing shut), `Microsoft.Maui.Controls.Compatibility` pinned to `8.0.100`; repo-root `global.json` pins the .NET 8 SDK.

## Capabilities

### New Capabilities
- `testing-infrastructure`: A three-layer automated test setup — isolated unit tests, container-backed API-integration tests, and a Windows-VM FlaUI bot harness — with an xUnit category convention (`Live` / `Integration`) that keeps `dotnet test` green on a plain checkout and in CI, plus a `.slnf` + GitHub Actions workflow that build and test the Linux-buildable projects on every push/PR.

## Non-goals

- Concrete bot scenarios (create pedido, weigh, etc.) and broad `AutomationProperties.AutomationId` coverage in the MAUI XAML — a follow-up change.
- The concurrency-`409` integration test (needs deterministic two-context orchestration) — deferred.
- A self-hosted Windows CI runner to fold the bot suite into CI — deferred.
- React admin frontend (`BasculaUi`) tests.
- Any change to production runtime behaviour — the csproj edits are build configuration and bug fixes only.

## Impact

**Affected terminals:** none — test-only. The `BasculaInterface.csproj` changes are build config: `WinExe` is a fix, the Android head is dropped (never deployed to a terminal), MAUI Compatibility is version-pinned.
**CI:** new `backend-tests.yml`; the existing `claude-code-review.yml` is untouched.
**Repo layout:** new `BasculaBotTests` project (in `.sln`, not in `BasculaInterface.CI.slnf`); `BasculaTerminalTest` reorganised into `Unit/ Integration/ Live/ TestDoubles/`; new `scripts/vm/`; new `global.json`.
**Packages:** `BasculaTerminalTest` adds NSubstitute, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`. `BasculaBotTests` adds FlaUI.
**Local dev:** integration tests need a Docker-compatible engine (Docker, or rootless Podman with its socket enabled); without one, run `--filter "Category!=Live&Category!=Integration"`.
