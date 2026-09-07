# VM bot suite

Automated UI smoke tests that drive the real Windows BasculaInterface app with
[FlaUI](https://github.com/FlaUI/FlaUI), replacing the manual click-through before a release.
Runs **only on the Windows VM** (`win10-maui-dev`) — FlaUI + a compiled MAUI Windows app.

## Status: step 1 (proof of concept)

Right now the suite has one test — `AppLaunchSmokeTests` — which just launches the app and
checks FlaUI can see the login screen. Once that's confirmed green on the VM we add
AutomationIds to the app's XAML and build out real scenarios (configure host, create pedido,
convert line, enter a manual weight, verify).

## Prerequisites on the VM

- **.NET 8 SDK** — the repo pins to it via `global.json`. Check with `dotnet --list-sdks`;
  if there's no `8.0.x`, install it: `winget install Microsoft.DotNet.SDK.8`
  (a newer SDK like .NET 10 can be installed alongside, `global.json` picks the 8.x one).
- MAUI Windows workload: `dotnet workload install maui`
- The two DB connection strings already in the machine environment
  (`PostgresWeightConnection`, `ContpaqSQLConnection`) — same as for a normal run.

No scale hardware needed: the app's serial-port failure is already handled, and the bot uses
the app's **manual weight** mode (Settings → "Manual") to type weights into the UI.

## Run

```bat
REM from the repo root on the VM — the .cmd wrapper dodges PowerShell execution policy
scripts\vm\run-bot-suite.cmd
```

or directly:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vm\run-bot-suite.ps1
```

> If you copied the repo via the `Z:` share, Windows tags the files as "from another computer"
> and even `RemoteSigned` will refuse the `.ps1`. Either use the `.cmd` above, or clear the tag
> once: `Get-ChildItem -Recurse | Unblock-File`. Cloning with `git` instead of copying avoids
> this entirely (and makes `git pull` the update path).

This builds `BasculaInterface` for `net8.0-windows`, runs the bot suite against the fresh exe,
and drops a `bot-tests.trx` plus screenshots in `artifacts/bot-suite/`.

Add `-StartApi` to also spin up `BasculaTerminalApi` on a dedicated port (5999) for the
scenarios that need it — the launch smoke test doesn't.

## How the app is built for the bot

Your normal deploy produces a **signed MSIX package**. FlaUI drives a plain `.exe`, so the script
publishes the repo README's "avoid MSIX" recipe instead, passed as command-line flags so the
csproj stays the single source of truth for the MSIX build:

| flag | why |
| --- | --- |
| `-p:WindowsPackageType=None` | unpackaged — no MSIX identity (`REGDB_E_CLASSNOTREG` otherwise) |
| `-p:OutputType=WinExe` | GUI subsystem — the csproj default is console `Exe`, which makes the window open then immediately close |
| `-p:WindowsAppSDKSelfContained=true` | bundle the Windows App Runtime so the VM needs nothing installed |
| `--self-contained true -r win-x64` | bundle the .NET runtime too |

If the self-contained Windows App Runtime ever misbehaves, the alternative is
`-p:WindowsAppSDKSelfContained=false` + install the runtime on the VM once
(`winget install Microsoft.WindowsAppRuntime.1.5`, or click "Yes" on the prompt).

## Notes

- **SQL Server is safe.** `ContpaqiSQLContext` throws on any `SaveChanges`, so bot runs can
  only read from it.
- **Postgres accumulates test data.** Bot-created pedidos/weight entries stay in the dev DB.
  Once scenarios are built they'll tag their rows (vehicle plate `BOT-TEST…`) and delete what
  they can via the API on the way out.
- If FlaUI 5.0 misbehaves on the VM's Windows build, drop `FlaUI.Core` / `FlaUI.UIA3` to
  `4.0.0` in `BasculaBotTests.csproj`.
