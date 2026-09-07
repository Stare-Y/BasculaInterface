#requires -Version 5.1
<#
.SYNOPSIS
  Builds the Windows BasculaInterface app and runs the FlaUI bot suite against it, on the VM.

.DESCRIPTION
  Step 1 of the bot suite is just proving FlaUI can drive the app. This script:
    1. publishes BasculaInterface as an unpackaged, WinAppSDK-self-contained Windows app
    2. points the bot suite at that exe (BASCULA_APP_EXE)
    3. optionally starts BasculaTerminalApi on a dedicated test port (-StartApi)
    4. runs `dotnet test BasculaBotTests`, writing a .trx + screenshots to -ArtifactsDir
    5. tears the API back down

  The API reads BOTH database connection strings from machine environment variables
  (PostgresWeightConnection, ContpaqSQLConnection) - same as always. The ContpaqiSQL
  context is read-only by construction (it throws on any SaveChanges), so bot runs
  cannot write to SQL Server. The Postgres dev DB does accumulate bot-created rows.

.EXAMPLE
  pwsh scripts/vm/run-bot-suite.ps1

.EXAMPLE
  pwsh scripts/vm/run-bot-suite.ps1 -StartApi -ApiPort 5999
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$TargetFramework = "net8.0-windows10.0.19041.0",
    [switch]$StartApi,
    [int]$ApiPort = 5999,
    [string]$ArtifactsDir
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$backend  = Join-Path $repoRoot "src\backend"
if (-not $ArtifactsDir) { $ArtifactsDir = Join-Path $repoRoot "artifacts\bot-suite" }
New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

Write-Host "== Publishing BasculaInterface ($Configuration / $TargetFramework) ==" -ForegroundColor Cyan
$appProj = Join-Path $backend "BasculaInterface\BasculaInterface.csproj"
# Must be `publish`, not `build`: your normal path packages a signed MSIX, but FlaUI drives a
# bare .exe. An UNPACKAGED, WinAppSDK-self-contained *publish* is the only flavour that starts
# standalone - `dotnet build` output for an unpackaged app opens then immediately exits. Your
# MSIX deploy path is untouched (it rebuilds with its own props).
$pubDir = Join-Path $backend "BasculaInterface\bin\bot-publish"
if (Test-Path $pubDir) { Remove-Item $pubDir -Recurse -Force }
# Unpackaged (README's "avoid MSIX" recipe), passed as flags so the csproj stays the single
# source of truth for the signed-MSIX deploy. (OutputType=WinExe can't go here - it would hit
# the class libraries too - it lives in BasculaInterface.csproj.)
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
if ($StartApi) {
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
if ($StartApi) { $env:BASCULA_API_URL = "http://localhost:$ApiPort" }

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
