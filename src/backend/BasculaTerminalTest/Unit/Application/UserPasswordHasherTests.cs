using Core.Application.Security;

namespace BasculaTerminalTest.Unit.Application
{
    /// <summary>
    /// Salted PBKDF2 hashing for real per-user passwords (issue #134) — deliberately distinct from
    /// <see cref="PasswordHasher"/>'s unsalted, deterministic SHA-256, which cannot verify a
    /// per-user password (see <see cref="UserPasswordHasher"/>'s own doc comment).
    /// </summary>
    public class UserPasswordHasherTests
    {
        [Fact]
        public void Verify_accepts_the_correct_plaintext_password()
        {
            string hash = UserPasswordHasher.Hash("correct-password");

            Assert.True(UserPasswordHasher.Verify("correct-password", hash));
        }

        [Fact]
        public void Verify_rejects_a_wrong_plaintext_password()
        {
            string hash = UserPasswordHasher.Hash("correct-password");

            Assert.False(UserPasswordHasher.Verify("wrong-password", hash));
        }

        [Fact]
        public void Hash_is_salted_so_the_same_password_produces_different_hashes()
        {
            string hash1 = UserPasswordHasher.Hash("same-password");
            string hash2 = UserPasswordHasher.Hash("same-password");

            Assert.NotEqual(hash1, hash2);
            Assert.True(UserPasswordHasher.Verify("same-password", hash1));
            Assert.True(UserPasswordHasher.Verify("same-password", hash2));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-enough-parts")]
        [InlineData("a.b.c.d")]
        [InlineData("notanumber.c2FsdA==.c3Via2V5")]
        public void Verify_returns_false_instead_of_throwing_for_a_malformed_stored_hash(string malformed)
        {
            Assert.False(UserPasswordHasher.Verify("anything", malformed));
        }

        [Fact]
        public void Hash_throws_for_an_empty_password()
        {
            Assert.Throws<ArgumentException>(() => UserPasswordHasher.Hash(string.Empty));
        }
    }
}
