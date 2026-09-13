#requires -Version 5.1
<#
.SYNOPSIS
  Generates a strong random secret for AuthSettings:JwtSigningKey, and optionally sets it as a
  persistent environment variable.

.DESCRIPTION
  BasculaTerminalApi refuses to start outside Development if AuthSettings:JwtSigningKey is still
  the placeholder from appsettings.json (Program.cs's startup guard, fix-user-authentication
  follow-up). Set a real value via the AuthSettings__JwtSigningKey environment variable (the double
  underscore is ASP.NET Core configuration's standard section:key separator) - it overrides
  appsettings.json regardless of whether the placeholder entry is left in the file or removed.

  Generates 64 random bytes (512 bits) via RNGCryptoServiceProvider - not the newer
  RandomNumberGenerator.Fill/GetBytes static APIs, which aren't available on Windows PowerShell 5.1
  (.NET Framework), only on PowerShell 7+ (.NET).

  Run this ONCE per environment and keep the value - it isn't meant to be regenerated on every
  deploy. Changing it later immediately invalidates every currently-issued login token (everyone
  gets logged out and has to log back in with their unchanged password); it has no effect on
  stored user passwords, which are hashed separately per-user (see New-UserPasswordHash.ps1).

.PARAMETER SetMachineEnvironmentVariable
  If passed, also calls [System.Environment]::SetEnvironmentVariable(..., "Machine") to persist the
  generated secret as AuthSettings__JwtSigningKey. Requires an elevated (Run as Administrator)
  PowerShell. The API process must be restarted afterward to pick it up - environment variables are
  only read at process start.

.EXAMPLE
  pwsh scripts/auth/New-JwtSigningKey.ps1
  Just prints a generated secret - set it yourself however you prefer.

.EXAMPLE
  pwsh scripts/auth/New-JwtSigningKey.ps1 -SetMachineEnvironmentVariable
  Generates a secret and sets it as a persistent machine-level environment variable directly
  (elevated PowerShell required). Restart the API/Windows Service afterward.
#>
[CmdletBinding()]
param(
    [switch]$SetMachineEnvironmentVariable
)

$ErrorActionPreference = "Stop"

$KeySizeBytes = 64

$bytes = New-Object byte[] $KeySizeBytes
$rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()
try {
    $rng.GetBytes($bytes)
}
finally {
    $rng.Dispose()
}

$secret = [Convert]::ToBase64String($bytes)

Write-Host ""
Write-Host "Generated AuthSettings:JwtSigningKey:" -ForegroundColor Cyan
Write-Host $secret
Write-Host ""

if ($SetMachineEnvironmentVariable) {
    [System.Environment]::SetEnvironmentVariable("AuthSettings__JwtSigningKey", $secret, "Machine")
    Write-Host "Set AuthSettings__JwtSigningKey as a persistent machine environment variable." -ForegroundColor Green
    Write-Host "Restart the API (or the Windows Service, if installed as one) for it to take effect." -ForegroundColor Yellow
}
else {
    Write-Host "Not set anywhere yet. To set it yourself as a persistent machine variable (elevated PowerShell):" -ForegroundColor Yellow
    Write-Host '  [System.Environment]::SetEnvironmentVariable("AuthSettings__JwtSigningKey", $secret, "Machine")'
    Write-Host "Then restart the API/service - environment variables are only read at process start."
}
