using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Application.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public static IEnumerable<ProductoDto> BuildFromBaseEntity(IEnumerable<Producto> productos)
        {
            return productos.Select(p => new ProductoDto
            {
                Id = p.CIDPRODUCTO,
                Nombre = p.CNOMBREPRODUCTO
            });
        }
    }
}
