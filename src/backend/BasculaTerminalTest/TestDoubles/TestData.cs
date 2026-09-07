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
        /// <summary>SHA-256 (hex, lowercase) of the plaintext <c>"password"</c>.</summary>
        public const string PasswordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8";

        public static WeightSettings WeightSettings(string changeProductPasswordHash = PasswordHash) => new()
        {
            CompanyName = "Test Co.",
            ChangeProductPasswordHash = changeProductPasswordHash,
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
