using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Core.Application.Security
{
    /// <summary>
    /// Salted PBKDF2 hashing for real per-user passwords (issue #134). Deliberately separate from
    /// the unsalted, single-shared-secret <see cref="PasswordHasher"/> used by the now-retired
    /// <c>ChangeProductPasswordHash</c> stopgap — a per-user salt cannot be verified by a
    /// client-side string comparison, so the server verifies the plaintext password directly
    /// (design.md Decision 2 of add-user-authentication-and-audit-log; confirmed acceptable given
    /// the local-network-only deployment).
    /// </summary>
    public static class UserPasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int SubkeySize = 32; // 256 bit
        private const int Iterations = 100_000;

        /// <summary>Produces a self-describing string: "{iterations}.{base64 salt}.{base64 subkey}".</summary>
        public static string Hash(string plaintextPassword)
        {
            if (string.IsNullOrEmpty(plaintextPassword))
                throw new ArgumentException("La contraseña no puede estar vacía.", nameof(plaintextPassword));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] subkey = KeyDerivation.Pbkdf2(plaintextPassword, salt, KeyDerivationPrf.HMACSHA256, Iterations, SubkeySize);

            return string.Join('.', Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(subkey));
        }

        /// <summary>Verifies a plaintext password against a hash produced by <see cref="Hash"/>.
        /// Returns false (never throws) for a malformed stored hash.</summary>
        public static bool Verify(string plaintextPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(plaintextPassword) || string.IsNullOrEmpty(storedHash))
                return false;

            string[] parts = storedHash.Split('.');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out int iterations))
                return false;

            byte[] salt;
            byte[] expectedSubkey;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedSubkey = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actualSubkey = KeyDerivation.Pbkdf2(plaintextPassword, salt, KeyDerivationPrf.HMACSHA256, iterations, expectedSubkey.Length);

            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
    }
}
