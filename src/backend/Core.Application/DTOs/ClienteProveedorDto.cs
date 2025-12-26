namespace Core.Application.DTOs
{
    public class ClienteProveedorDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = null!;
        public string? RFC { get; set; } = null;
        public double CreditLimit { get; set; }
        public double Debt { get; set; }
        public double AvailableCredit => CreditLimit - Debt;
        public bool OrderRequestAllowed { get; set; } = false;
        public bool IgnoreCreditLimit { get; set; } = false;
        public bool IsProvider { get; set; }
    }
}   
