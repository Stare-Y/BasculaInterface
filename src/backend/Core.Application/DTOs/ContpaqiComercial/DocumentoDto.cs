namespace Core.Application.DTOs.ContpaqiComercial
{
    public record DocumentoDto
    {
        public int NumMoneda { get; init; } = 0;
        public double TipoCambio { get; init; } = 0;
        public double Importe { get; init; } = 0;
        public double DescuentoDoc1 { get; init; } = 0;
        public double DescuentoDoc2 { get; init; } = 0;
        public int SistemaOrigen { get; init; } = 0;
        public required string CodConcepto { get; init; }
        public required string Serie { get; init; }
        /// <summary>
        /// REQUIRED Format: "MM/dd/yyyy"
        /// </summary>
        public required DateTime Fecha { get; init; }
        public required string CodigoCteProv { get; init; }
        public string RazonSocial { get; init; } = string.Empty;
        public string CodigoAgente { get; init; } = string.Empty;
        public string Referencia { get; init; } = string.Empty;
        public int Afecta { get; init; } = 0;
        public double Gasto1 { get; init; } = 0;
        public double Gasto2 { get; init; } = 0;
        public double Gasto3 { get; init; } = 0;
        public string? cObservaciones { get; init; }
        public string? cTextoExtra1 { get; init; }
        public string? cTextoExtra2 { get; init; }
        public string? cTextoExtra3 { get; init; }
        public MovimientoDto[] Movimientos { get; init; } = [];
    }
}
