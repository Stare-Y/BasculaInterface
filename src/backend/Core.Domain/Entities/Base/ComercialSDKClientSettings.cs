namespace Core.Domain.Entities.Base
{
    public class ComercialSDKClientSettings
    {
        public required string DefaultSerie {  get; set; }
        public required string DefaultConcepto { get; set; }
        public required string DefaultAlmacen { get; set; }
        public required string ProveedorSerie { get; set; }
        public required string ProveedorConcepto { get; set; }
        public required string ProveedorAlmacen { get; set; }
        public required string ApiUrl { get; set; }
        public required string TargetEmpresa { get; set; }
    }
}
