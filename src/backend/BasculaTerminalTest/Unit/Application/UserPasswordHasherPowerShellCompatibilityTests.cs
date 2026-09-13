using System.Security.Cryptography;
using Core.Application.Security;

namespace BasculaTerminalTest.Unit.Application
{
    /// <summary>
    /// scripts/auth/New-UserPasswordHash.ps1 reproduces UserPasswordHasher's algorithm using
    /// Rfc2898DeriveBytes + HashAlgorithmName.SHA256 (Windows PowerShell 5.1/.NET Framework
    /// compatible) instead of the newer KeyDerivation.Pbkdf2 the C# hasher itself uses (ASP.NET
    /// Core-only). Both are standard PBKDF2-HMACSHA256, so they must produce byte-identical output
    /// for the same password/salt/iterations — this pins that down so a future change to
    /// UserPasswordHasher's parameters that isn't mirrored in the .ps1 script fails loudly here
    /// instead of producing a hash the API silently rejects.
    /// </summary>
    public class UserPasswordHasherPowerShellCompatibilityTests
    {
        [Theory]
        [InlineData("s3cret-password")]
        [InlineData("a")]
        [InlineData("a very long password with spaces and Ñ, é, 123!")]
        public void A_hash_built_the_way_the_ps1_script_does_it_verifies_successfully(string password)
        {
            // Mirrors New-UserPasswordHash.ps1 exactly: 16-byte salt, 100_000 iterations, 32-byte
            // subkey, "{iterations}.{base64 salt}.{base64 subkey}".
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using Rfc2898DeriveBytes deriveBytes = new(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] subkey = deriveBytes.GetBytes(32);

            string hash = string.Join('.', 100_000, Convert.ToBase64String(salt), Convert.ToBase64String(subkey));

            Assert.True(UserPasswordHasher.Verify(password, hash));
        }

        [Fact]
        public void A_hash_built_the_way_the_ps1_script_does_it_rejects_the_wrong_password()
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using Rfc2898DeriveBytes deriveBytes = new("correct-password", salt, 100_000, HashAlgorithmName.SHA256);
            byte[] subkey = deriveBytes.GetBytes(32);

            string hash = string.Join('.', 100_000, Convert.ToBase64String(salt), Convert.ToBase64String(subkey));

            Assert.False(UserPasswordHasher.Verify("wrong-password", hash));
        }
    }
}
