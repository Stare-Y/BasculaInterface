## 0. Prep

- [x] 0.1 Add `public partial class Program {}` marker to `BasculaTerminalApi/Program.cs` (for `WebApplicationFactory<Program>`)
- [x] 0.2 Move `PasswordHasher` from `BasculaInterface/Services` to `Core.Application/Security` (pure SHA-256, no MAUI dep); update the 4 call sites in `DetailedWeightViewModel`
- [x] 0.3 Fix `BasculaClient` live test: `"ReceiveNumber"` → `"ReceiveLecture"` (was silently dead); add `[Trait("Category","Live")]`; move to `BasculaTerminalTest/Live/`
- [x] 0.4 Reorganise `BasculaTerminalTest` into `Unit/ Integration/ Live/ TestDoubles/`; add project references (`Core.Domain`, `Core.Application`, `Infrastructure`, `BasculaTerminalApi`); add `NSubstitute`
- [x] 0.5 Add repo-root `global.json` pinning the .NET 8 SDK (`rollForward: latestMinor`)

## 1. Unit layer

- [x] 1.1 `PedidoServiceConvertLineToWeightTests` — all branches of `ConvertLineToWeightAsync` (closed line, non-positive / over-pending target, append to concluded entry, missing/non-hidden almacén target, received-amount computation ignoring unloaded/deleted details, append vs. new discharge entry) ⚠️ HIGH-RISK path (pending calculation feeds `BruteWeight`)
- [x] 1.2 `WeightServicePasswordGateTests` — `ChangeDetailProductAsync` / `ChangePartnerAsync` / `ChangeDetailAmountAsync` / `DeleteDetailSafelyAsync` × {wrong password → `UnauthorizedAccessException`, no repo call}, {unconfigured hash rejected}, {correct password case-insensitive → proceeds}
- [x] 1.3 `WeightLogisticServiceTests` — blank device id, grant on free scale, block second device, holder renew, non-holder release refused, holder release frees the scale
- [x] 1.4 `PasswordHasherTests` — known SHA-256 vectors, null-as-empty, 64-char lowercase hex, determinism
- [x] 1.5 `TestDoubles/TestData` — builders for `WeightSettings` / `ComercialSDKClientSettings` (both carry `required` members)

## 2. CI

- [x] 2.1 `src/backend/BasculaInterface.CI.slnf` — Linux-buildable projects only (`Core.Domain`, `Core.Application`, `Infrastructure`, `BasculaTerminalApi`, `BasculaTerminalTest`); excludes the MAUI app and `BasculaBotTests`
- [x] 2.2 `.github/workflows/backend-tests.yml` — ubuntu-latest, `setup-dotnet` 8.0.x, `dotnet build`/`test` the `.slnf` with `--filter "Category!=Live"`, `trx` logger, `dorny/test-reporter`, artifact upload
- [x] 2.3 Verified locally: `dotnet test <slnf> --filter "Category!=Live"` → 47 passed

## 3. API-integration layer

- [x] 3.1 `Integration/BasculaApiFactory` — `WebApplicationFactory<Program>` + `Testcontainers.PostgreSql` (`IAsyncLifetime`), sets `PostgresWeightConnection` / dummy `ContpaqSQLConnection` before host build, swaps `IBasculaService` → `FakeBasculaService`, `IProductRepo`/`IClienteProveedorRepo`/`IDocumentRepo` → fakes, `PostConfigure<WeightSettings>` to a known password hash; `IntegrationCollection` fixture
- [x] 3.2 `TestDoubles/FakeBasculaService` (raise synthetic `OnBasculaRead`), `TestDoubles/FakeContpaqiRepos`
- [x] 3.3 `Integration/ModuleInit` — `TESTCONTAINERS_RYUK_DISABLED=true` (rootless-Podman compatible)
- [x] 3.4 `PedidoPartialFulfillmentFlowTests` — convert line → discharge entry against provider/target; record partial weight → line `ReceivedAmount`/`PendingAmount` update, not concluded; convert over pending → `400`
- [x] 3.5 `WeightPasswordGateHttpTests` — wrong hash → `400` "Contraseña incorrecta."; configured hash → `200` + amount updated
- [x] 3.6 `ScaleBroadcastTests` — `FakeBasculaService.RaiseWeight` → connected SignalR client receives `"ReceiveLecture"`
- [x] 3.7 `HttpAssert` helper (body in failure message); `BasculaTerminalTest/README.md` (how to run each layer, Podman socket setup)
- [x] 3.8 Verified locally against a Podman Postgres container: 6 integration tests green (47 total)
- [ ] 3.9 Concurrency-`409` integration test — deferred (needs deterministic two-context orchestration)

## 4. VM bot harness

- [x] 4.1 `src/backend/BasculaBotTests` project — `net8.0-windows`, FlaUI 5 + xUnit; added to `.sln`, excluded from `BasculaInterface.CI.slnf`
- [x] 4.2 `AppDriver` — launch the published exe with redirected output, enumerate every top-level window the process owns, wait for real content, write `diagnostics.txt` (console output, exit code, per-window control trees) + `window-*.png`
- [x] 4.3 `AppLaunchSmokeTests` — launch → main window readable (≥ 3 descendants) → `Login` button present
- [x] 4.4 `scripts/vm/run-bot-suite.ps1` + `.cmd` wrapper (execution-policy / Mark-of-the-Web safe) — `dotnet publish` unpackaged `WinExe` self-contained, point `BASCULA_APP_EXE` at it, run the suite, `-StartApi` to also host `BasculaTerminalApi` on a test port; `scripts/vm/README.md`
- [x] 4.5 `BasculaInterface.csproj` — Windows-only (`net8.0-android` dropped), `OutputType` `Exe` → `WinExe`, `Microsoft.Maui.Controls.Compatibility` pinned `8.0.100`
- [x] 4.6 Verified on `win10-maui-dev`: smoke test passes — FlaUI reads the login screen (15 elements, `Login` button, texts, images); screenshot confirms

## 5. Follow-ups (separate change)

- [ ] 5.1 Add `AutomationProperties.AutomationId` to the pedido / weighing / settings controls the scenarios touch
- [ ] 5.2 Bot scenarios: configure host + enable Manual weight → create pedido → convert line → type manual weight → assert pending drops
- [ ] 5.3 Evaluate a self-hosted Windows GitHub Actions runner to fold the bot suite into CI
