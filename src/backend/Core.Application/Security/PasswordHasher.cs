using System.Security.Cryptography;
using System.Text;

namespace Core.Application.Security
{
    /// <summary>
    /// SHA-256 hashing for the "change product" password gate (issue #122).
    /// The plaintext is never sent to the API — only this hash is, and the server
    /// compares it against the configured <c>ChangeProductPasswordHash</c>.
    /// Lives in Core.Application so both the MAUI client and server-side tests use
    /// the exact same algorithm.
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
