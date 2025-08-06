using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Application.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public int IdValorClasificacion6 { get; set; }
    }
}
