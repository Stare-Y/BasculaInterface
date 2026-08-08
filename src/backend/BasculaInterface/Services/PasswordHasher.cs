using System.Security.Cryptography;
using System.Text;

namespace BasculaInterface.Services
{
    /// <summary>
    /// Client-side hashing for the "change product" password gate (issue #122).
    /// Plaintext is never sent to the API — only this hash is.
    /// </summary>
    public static class PasswordHasher
    {
        public static string HashSha256Hex(string plaintext)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext ?? string.Empty));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
