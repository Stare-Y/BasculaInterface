using Core.Domain.Entities.Base;
using Core.Application.Settings;

namespace BasculaTerminalTest.TestDoubles
{
    /// <summary>
    /// Small builders for settings/config objects that carry <c>required</c> members, so
    /// individual tests don't have to restate boilerplate just to construct a service.
    /// </summary>
    internal static class TestData
    {
        /// <summary>The seeded Sudo test user's credentials (see <see cref="BasculaTerminalTest.Integration.BasculaApiFactory"/>).
        /// Sudo bypasses every authorization/permission check, so this identity is the right
        /// default "correct" gate credential in tests that aren't specifically exercising a
        /// non-Sudo permission boundary.</summary>
        public const string SudoUsername = "sudo-test";
        public const string SudoUserCode = "SUDO1";
        public const string SudoPassword = "test-password";

        public static WeightSettings WeightSettings() => new()
        {
            CompanyName = "Test Co.",
        };

        public static ComercialSDKClientSettings ComercialSdkSettings() => new()
        {
            DefaultSerie = "A",
            DefaultConcepto = "C",
            DefaultAlmacen = "1",
            ProveedorSerie = "B",
            ProveedorConcepto = "D",
            ProveedorAlmacen = "2",
            ApiUrl = "http://localhost/sdk",
            TargetEmpresa = "TEST",
        };
    }
}
