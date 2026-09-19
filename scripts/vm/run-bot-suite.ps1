#requires -Version 5.1
<#
.SYNOPSIS
  Builds the Windows BasculaInterface app and runs the FlaUI bot suite against it, on the VM.

.DESCRIPTION
  This script:
    1. validates the bot bootstrap/role credential environment variables are set (fails fast,
       before publishing anything, if they're not — the roleplays can't log in without them)
    2. publishes BasculaInterface as an unpackaged, WinAppSDK-self-contained Windows app
    3. points the bot suite at that exe (BASCULA_APP_EXE)
    4. starts BasculaTerminalApi on a dedicated test port — the roleplays need a live API to log
       in and provision test users against, so this is the default now, not the old -StartApi
       opt-in (pass -NoApi to skip it and go back to launch-only mode)
    5. runs `dotnet test BasculaBotTests`, writing a .trx + screenshots to -ArtifactsDir
    6. tears the API back down

  The API reads BOTH database connection strings from machine environment variables
  (PostgresWeightConnection, ContpaqSQLConnection) - same as always. The ContpaqiSQL
  context is read-only by construction (it throws on any SaveChanges), so bot runs
  cannot write to SQL Server. The Postgres dev DB does accumulate bot-created rows.

.EXAMPLE
  pwsh scripts/vm/run-bot-suite.ps1

.EXAMPLE
  pwsh scripts/vm/run-bot-suite.ps1 -NoApi
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$TargetFramework = "net8.0-windows10.0.19041.0",
    [switch]$NoApi,
    # BasculaTerminalApi/appsettings.json hardcodes Kestrel:Endpoints:Http:Url to
    # "http://*:6969" for every environment, which overrides `dotnet run --urls` below (ASP.NET
    # Core logs "Overriding address(es) ... Binding to endpoints defined via IConfiguration" and
    # binds to 6969 regardless of what's passed here). Default matches that fixed port; the
    # --urls flag is still passed below in case that appsettings.json override is ever removed.
    [int]$ApiPort = 6969,
    [string]$ArtifactsDir
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$backend  = Join-Path $repoRoot "src\backend"
if (-not $ArtifactsDir) { $ArtifactsDir = Join-Path $repoRoot "artifacts\bot-suite" }
New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

if (-not $NoApi) {
    # Fail fast, before spending time on a publish, if the bootstrap/role credentials the
    # provisioning routine needs aren't set (spec: "Missing bootstrap credentials fail fast").
    $requiredVars = @("BasculaBotAdminIdentifier", "BasculaBotAdminPassword", "BasculaBotRolePassword")
    $missing = $requiredVars | Where-Object { -not (Get-Item "Env:$_" -ErrorAction SilentlyContinue) }
    if ($missing) {
        throw "Missing required environment variable(s) for the bot suite: $($missing -join ', '). " +
              "See scripts/vm/README.md. (Pass -NoApi to run launch-only, without provisioning.)"
    }
}

Write-Host "== Publishing BasculaInterface ($Configuration / $TargetFramework) ==" -ForegroundColor Cyan
$appProj = Join-Path $backend "BasculaInterface\BasculaInterface.csproj"
# Must be `publish`, not `build`: `dotnet build` output for an unpackaged app opens then
# immediately exits. An unpackaged, WinAppSDK-self-contained *publish* is the only flavour
# that starts standalone. This is the same unpackaged shape as the normal deploy
# (WindowsPackageType=None lives permanently in BasculaInterface.csproj) - the bot just needs
# its own fresh publish output, separate from wherever the normal deploy's exe lands.
$pubDir = Join-Path $backend "BasculaInterface\bin\bot-publish"
if (Test-Path $pubDir) { Remove-Item $pubDir -Recurse -Force }
# WindowsPackageType=None below is redundant with the csproj default - kept as
# belt-and-suspenders in case that default ever changes.
#   WindowsPackageType=None      -> unpackaged, no MSIX identity
#   WindowsAppSDKSelfContained   -> bundle the Windows App Runtime so the VM needs nothing
dotnet publish $appProj -c $Configuration -f $TargetFramework -r win-x64 --self-contained true `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:AppxPackageSigningEnabled=false `
    -o $pubDir
if ($LASTEXITCODE -ne 0) { throw "MAUI publish failed." }

$appExe = Get-ChildItem -Path $pubDir -Recurse -Filter "BasculaInterface.exe" |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $appExe) { throw "Could not find BasculaInterface.exe under $pubDir." }
Write-Host "   app: $($appExe.FullName)"

$apiProc = $null
if (-not $NoApi) {
    Write-Host "== Starting BasculaTerminalApi on http://localhost:$ApiPort ==" -ForegroundColor Cyan
    $apiProj = Join-Path $backend "BasculaTerminalApi\BasculaTerminalApi.csproj"
    $apiProc = Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $apiProj, "-c", $Configuration, "--urls", "http://localhost:$ApiPort") `
        -PassThru -WindowStyle Hidden

    $up = $false
    foreach ($i in 1..30) {
        Start-Sleep -Seconds 2
        try {
            $tcp = New-Object System.Net.Sockets.TcpClient
            $tcp.Connect("localhost", $ApiPort); $tcp.Close(); $up = $true; break
        } catch { }
    }
    if (-not $up) { Write-Warning "API didn't answer on port $ApiPort after 60s; continuing anyway." }
    else { Write-Host "   API is up." }
}

$env:BASCULA_APP_EXE = $appExe.FullName
$env:BASCULA_BOT_ARTIFACTS = $ArtifactsDir
if (-not $NoApi) { $env:BASCULA_API_URL = "http://localhost:$ApiPort" }
# BasculaBotAdminIdentifier / BasculaBotAdminPassword / BasculaBotRolePassword are read straight
# from the machine/session environment by BotUserProvisioner - `dotnet test` below inherits them
# from this process like any child process, no explicit pass-through needed.

Write-Host "== Running bot suite ==" -ForegroundColor Cyan
$trx = Join-Path $ArtifactsDir "bot-tests.trx"
dotnet test (Join-Path $backend "BasculaBotTests\BasculaBotTests.csproj") `
    -c $Configuration `
    --logger "trx;LogFileName=$trx" `
    --results-directory $ArtifactsDir
$testExit = $LASTEXITCODE

if ($apiProc) {
    Write-Host "== Stopping API ==" -ForegroundColor Cyan
    try { Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue } catch { }
    Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
        Where-Object { $_.CommandLine -match "BasculaTerminalApi" -and $_.CommandLine -match "$ApiPort" } |
        ForEach-Object { try { Stop-Process -Id $_.ProcessId -Force } catch { } }
}

Write-Host ""
if ($testExit -eq 0) { Write-Host "BOT SUITE PASSED" -ForegroundColor Green }
else { Write-Host "BOT SUITE FAILED (exit $testExit) - see $ArtifactsDir" -ForegroundColor Red }
Write-Host "Artifacts (trx + screenshots): $ArtifactsDir"
exit $testExit
