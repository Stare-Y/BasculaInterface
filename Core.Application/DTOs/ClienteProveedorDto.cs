using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Application.DTOs
{
    public class ClienteProveedorDto
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = null!;
        public string? RFC { get; set; } = null;
    }
}
