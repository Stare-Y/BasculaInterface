# Design: Automated Test Foundations

## Context

Solo-developer project in production. Backend is Clean Architecture (`Core.Domain` / `Core.Application` / `Infrastructure`) behind `BasculaTerminalApi` (ASP.NET Core + SignalR, runs as a Windows Service). The MAUI client is a thin shell over the API. Dev happens on Linux; the MAUI app builds/runs only on Windows (a VM). Before this change: one broken, hardware-dependent test and an AI-review-only CI.

The three layers below are ordered by cost and by fidelity to "does the real thing work": unit (cheap, everywhere) → API-integration (real stack, no UI) → VM bot (real UI, Windows only).

## Goals / Non-Goals

**Goals:** a fast unit suite that runs on a plain checkout; an integration suite that exercises the real HTTP + SignalR + EF stack without hardware; a bot harness that can drive the actual Windows app; CI that runs the first two on every push/PR; one `dotnet test` command per situation.

**Non-Goals:** concrete bot scenarios, broad AutomationIds, the concurrency-409 test, a self-hosted Windows runner, frontend tests. (All follow-ups.)

## Decisions

### Decision 1: One backend test project, organised by folder
Keep `BasculaTerminalTest` as the single backend test project (small solution, one dev) with folders `Unit/`, `Integration/`, `Live/`, `TestDoubles/` rather than a project per layer. Keeps `dotnet test` trivial and avoids near-empty `.csproj` files. It references `Core.Domain`, `Core.Application`, `Infrastructure`, and `BasculaTerminalApi`.

### Decision 2: xUnit category traits gate what runs where
- `[Trait("Category","Live")]` — needs a running API + real scale (`BasculaClient`). Never in CI or a normal run.
- `[Trait("Category","Integration")]` — needs a Docker-compatible engine (Testcontainers).
- untagged — pure unit, runs anywhere.

Contract: `dotnet test --filter "Category!=Live"` is what CI runs; `--filter "Category!=Live&Category!=Integration"` is the Docker-less local run. This is the mechanism that keeps a plain checkout green.

### Decision 3: NSubstitute for mocking; plain `Assert` for assertions
NSubstitute over Moq (Moq's 2023 SponsorLink telemetry incident) — friendlier syntax for a dev without heavy mocking-library history. **No FluentAssertions** (went commercial-licensed in v8) — xUnit's built-in `Assert` covers the equality / exception / collection checks this suite needs.

### Decision 4: API-integration via `WebApplicationFactory<Program>` + Testcontainers Postgres
In-memory host, not a spawned process — one `dotnet test` run, full DI control, no port juggling. Testcontainers PostgreSQL (not EF InMemory) so `Program.cs`'s startup `db.Database.Migrate()` runs the real migrations and provider-specific SQL is exercised. Required app change: `public partial class Program {}` so the test assembly can name the entry point.

`AddPersistency` reads both connection strings straight from the environment and throws if absent, before any DI override runs — so the factory sets `PostgresWeightConnection` (to the container) and a dummy `ContpaqSQLConnection` before the host builds. `IBasculaService` (opens a serial port in its constructor) is swapped for `FakeBasculaService`; the three `ContpaqiSQLContext`-backed repos are swapped for empty fakes, which keeps that context — read-only by construction — from ever connecting.

### Decision 5: FlaUI for the VM bot, not WinAppDriver
FlaUI is a native .NET UI-Automation library — no separate driver service to babysit on an unattended VM, and WinAppDriver is in low-maintenance upstream. Trade-off: FlaUI is Windows-only (no transfer to Android UI automation later).

### Decision 6: The bot builds an unpackaged `WinExe` publish
The normal deploy is a signed MSIX. FlaUI drives a bare `.exe`, and a packaged WinUI exe can't start without package identity. `scripts/vm/run-bot-suite.ps1` therefore `dotnet publish`es with `-p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true --self-contained true -r win-x64`. The one prop that can't be a CLI flag (it would hit the class libraries) is `OutputType=WinExe` — that lives in `BasculaInterface.csproj` and is a straight fix: a GUI app built as a console `Exe` opens its window then immediately closes.

### Decision 7: No `SimulatedBasculaService`; the bot uses manual-weight mode
The VM has no scale and the API's serial-open failure is already handled. Rather than a new fake-scale service, the bot enables the app's built-in "Manual" weight mode (Settings) and types weights into the field that replaces the live reading. One less production-adjacent moving part.

### Decision 8: `global.json` pins the .NET 8 SDK
The VM was upgraded to the .NET 10 SDK, which hard-errors on the (now-EOL) `net8.0-android` workload and doesn't resolve `$(MauiVersion)` for a net8 target. Rather than migrate the whole app to net10 blind, `global.json` (`rollForward: latestMinor`) pins any installed 8.x SDK — matching CI and keeping the build reproducible.

## Test Surface

| Layer | Path | Runs on | Command |
| --- | --- | --- | --- |
| Unit | `BasculaTerminalTest/Unit/` | anywhere | `dotnet test --filter "Category!=Live&Category!=Integration"` |
| Integration | `BasculaTerminalTest/Integration/` | Docker/Podman | `dotnet test --filter "Category=Integration"` |
| CI (unit + integration) | `BasculaInterface.CI.slnf` | ubuntu-latest | `dotnet test <slnf> --filter "Category!=Live"` |
| Live hardware | `BasculaTerminalTest/Live/` | terminal PC + scale | `dotnet test --filter "Category=Live"` |
| VM bot | `BasculaBotTests/` | `win10-maui-dev` | `scripts\vm\run-bot-suite.cmd` |

## Reporting

`dotnet test --logger "trx;LogFileName=..."` (built-in). CI turns the `.trx` into a PR check via `dorny/test-reporter`. Local / VM runs rely on `dotnet test`'s own console summary plus the retained `.trx`. No dashboard.

## Risks / Trade-offs

- Integration tests need a container engine — flagged as a CI prerequisite (present on ubuntu-latest) and a local one (`README` documents the Podman socket setup).
- The Postgres dev DB accumulates bot/integration rows — acceptable per the owner; scenarios will tag and clean their rows.
- MAUI XAML changes for AutomationIds (follow-up) can't be verified from Linux — will be iterated on the VM.
- `BasculaBotTests` is in the `.sln` but not the CI `.slnf`; a `dotnet build` of the full solution on Linux fails on it and on the MAUI project — expected, hence the `.slnf`.

## Open Questions

- Whether the bot scenarios and a self-hosted Windows runner become one follow-up change or two.
