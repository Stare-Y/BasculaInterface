namespace Core.Application.DTOs.ContpaqiComercial
{
    public record MovimientoDto
    {
        public int Id { get; set; }
        public required string CodigoProducto { get; init; }
        public required string CodigoAlmacen { get; init; }
        public string CodigoClasificacion { get; init; } = string.Empty;
        public double Unidades { get; init; }
        public double Precio { get; init; }
        public double Costo { get; init; }
        public DateTime Fecha { get; init; }
        public string Referencia { get; init; } = string.Empty;
        public double Surtidas { get; init; }
        public bool EsGranel { get; init; }
    }
}
