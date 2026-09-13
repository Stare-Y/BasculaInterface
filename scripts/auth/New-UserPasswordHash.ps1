#requires -Version 5.1
<#
.SYNOPSIS
  Generates a PasswordHash value compatible with Core.Application.Security.UserPasswordHasher, for
  inserting or updating a User row's password directly in the database (e.g. seeding the first
  Sudo user - the app deliberately ships with no seeded accounts, design.md Decision 9 of
  add-user-authentication-and-audit-log).

.DESCRIPTION
  Reproduces UserPasswordHasher.Hash's exact algorithm: PBKDF2-HMACSHA256, 100,000 iterations, a
  16-byte random salt, a 32-byte derived subkey, formatted as "{iterations}.{base64 salt}.{base64
  subkey}" - store that whole string verbatim in the User row's PasswordHash column.

  Uses System.Security.Cryptography.Rfc2898DeriveBytes's HashAlgorithmName overload (available
  since .NET Framework 4.7.2) and RNGCryptoServiceProvider, both compatible with Windows
  PowerShell 5.1 - the newer KeyDerivation/RandomNumberGenerator APIs the C# hasher itself uses are
  ASP.NET Core-only and not available here, but standard PBKDF2-HMACSHA256 is the same construction
  either way, so the output verifies correctly against UserPasswordHasher.Verify.

  The password is read via -AsSecureString so it is never echoed to the console or captured in a
  terminal transcript/scrollback.

.EXAMPLE
  pwsh scripts/auth/New-UserPasswordHash.ps1
  Prompts for a password (masked input) and prints the PasswordHash string to store.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$SaltSize = 16
$SubkeySize = 32
$Iterations = 100000

$securePassword = Read-Host -Prompt "Password to hash" -AsSecureString
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $plaintextPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
}
finally {
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

if ([string]::IsNullOrEmpty($plaintextPassword)) {
    throw "La contraseña no puede estar vacía."
}

$salt = New-Object byte[] $SaltSize
$rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()
try {
    $rng.GetBytes($salt)
}
finally {
    $rng.Dispose()
}

$deriveBytes = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
    $plaintextPassword, $salt, $Iterations, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
try {
    $subkey = $deriveBytes.GetBytes($SubkeySize)
}
finally {
    $deriveBytes.Dispose()
}

$hash = "{0}.{1}.{2}" -f $Iterations, [Convert]::ToBase64String($salt), [Convert]::ToBase64String($subkey)

Write-Host ""
Write-Host "PasswordHash (store this exact string):" -ForegroundColor Cyan
Write-Host $hash
Write-Host ""
Write-Host "Example - inserting a first Sudo user (adjust the enum's underlying int if Role changes):" -ForegroundColor Yellow
Write-Host '  INSERT INTO "Users" ("Username", "UserCode", "PasswordHash", "Role", "InactivityTimeoutMinutes", "CreatedAt", "IsDeleted")'
Write-Host "  VALUES ('yourusername', 'YOURCODE', '<hash above>', 4, 2, NOW(), false);"
Write-Host "  -- Role 4 = Sudo per Core.Domain.Entities.Identity.Role's declared enum order at time of writing - verify against the enum before relying on the number."
