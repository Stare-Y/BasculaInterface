using Core.Application.Security;

namespace BasculaTerminalTest.Unit.Application
{
    public class PasswordHasherTests
    {
        [Theory]
        [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData("password", "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8")]
        [InlineData("Contraseña123", "4f5304d0a069388f5057c385b23b2514353c17db255d26397165fb2ba5873f7f")]
        public void HashSha256Hex_matches_known_vectors(string input, string expected)
        {
            Assert.Equal(expected, PasswordHasher.HashSha256Hex(input));
        }

        [Fact]
        public void HashSha256Hex_treats_null_as_empty_string()
        {
            Assert.Equal(PasswordHasher.HashSha256Hex(string.Empty), PasswordHasher.HashSha256Hex(null!));
        }

        [Fact]
        public void HashSha256Hex_returns_64_lowercase_hex_chars()
        {
            string hash = PasswordHasher.HashSha256Hex("anything at all");

            Assert.Equal(64, hash.Length);
            Assert.Matches("^[0-9a-f]{64}$", hash);
        }

        [Fact]
        public void HashSha256Hex_is_deterministic()
        {
            Assert.Equal(PasswordHasher.HashSha256Hex("repeat"), PasswordHasher.HashSha256Hex("repeat"));
        }
    }
}
