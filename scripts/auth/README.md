# Auth secrets helpers

Two small PowerShell 5.1-compatible scripts for the manual, no-seeding auth setup
(`add-user-authentication-and-audit-log`, design.md Decision 9 — there is no automatic seeding of
any account, and `AuthSettings:JwtSigningKey` ships as an unusable placeholder on purpose).

Both read/write only local variables and their own console output — neither touches the database
or the API directly.

## `New-JwtSigningKey.ps1`

Generates a strong random secret for `AuthSettings:JwtSigningKey` (the API refuses to start
outside `Development` if this is still the placeholder — see `Program.cs`'s startup guard). Run
once per environment; not meant to be regenerated on every deploy.

```powershell
pwsh scripts/auth/New-JwtSigningKey.ps1
# or, to also set it as a persistent machine environment variable directly (elevated PowerShell):
pwsh scripts/auth/New-JwtSigningKey.ps1 -SetMachineEnvironmentVariable
```

Set via the `AuthSettings__JwtSigningKey` environment variable (double underscore = ASP.NET Core's
section:key separator) — it overrides `appsettings.json` either way, whether or not the placeholder
entry is left in the file. Restart the API/Windows Service afterward; environment variables are
only read at process start. Changing this later logs everyone out (invalidates every issued token)
but never touches stored passwords — see below, they're a completely separate secret.

## `New-UserPasswordHash.ps1`

Generates a `PasswordHash` value for a `User` row, matching
`Core.Application.Security.UserPasswordHasher`'s exact algorithm (PBKDF2-HMACSHA256, 100,000
iterations, 16-byte salt, 32-byte subkey). Needed because there's no seeding — the first `Sudo`
user (and any account created outside the admin API) is inserted directly into the database.

```powershell
pwsh scripts/auth/New-UserPasswordHash.ps1
```

Prompts for the password as masked input (`-AsSecureString`) so it's never echoed to the console
or captured in a terminal transcript — paste the printed hash string verbatim into the `PasswordHash`
column.
