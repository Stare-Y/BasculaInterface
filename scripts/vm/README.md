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
- Bot bootstrap/role credentials, also as machine environment variables:
  - `BasculaBotAdminIdentifier` / `BasculaBotAdminPassword` — an existing `Admin`/`Sudo` account
    on the target DB; the suite logs in as it to provision the six `BOT*` test users.
  - `BasculaBotRolePassword` — the single shared password used to log in as each `BOT*` user.

No scale hardware needed: the app's serial-port failure is already handled, and the bot uses
the app's **manual weight** capture — the `CheckBoxUseManual` toggle on `WeightingScreen`,
gated by the `CanCaptureWeightManually` permission (Supervisor/Sudo by default) — to type
weights into the UI instead of Settings.

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

A live `BasculaTerminalApi` (port 6969 — `appsettings.json`'s Kestrel config hardcodes that for
every environment, overriding any `-ApiPort`/`--urls` value) starts automatically — the
roleplays need one to log in and provision the `BOT*` test users. Before publishing anything, the
script checks that `BasculaBotAdminIdentifier`, `BasculaBotAdminPassword`, and
`BasculaBotRolePassword` are set in the environment and fails fast with a clear message if not.
Pass `-NoApi` to skip the API and go back to launch-only mode (no login, no provisioning).

## How the app is built for the bot

The normal deploy is already unpackaged too — `BasculaInterface.csproj` sets
`WindowsPackageType=None` (and `OutputType=WinExe`) permanently, so there's no separate signed
MSIX path to avoid. FlaUI still needs its own publish output though (a fresh, self-contained
build dropped in `bin\bot-publish`), so the script passes a few flags on top of the csproj
defaults:

| flag | why |
| --- | --- |
| `-p:WindowsPackageType=None` | belt-and-suspenders — already the csproj default, kept here in case that ever changes |
| `-p:WindowsAppSDKSelfContained=true` | bundle the Windows App Runtime so the VM needs nothing installed |
| `-p:AppxPackageSigningEnabled=false` | no-op today (nothing gets signed once unpackaged) — harmless to keep |
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
