using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Application.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public int IdValorClasificacion6 { get; set; }
        public double Precio { get; set; }
        public ProductoDto(Producto productBase)
        {
            Id = productBase.CIDPRODUCTO;
            Nombre = productBase.CNOMBREPRODUCTO;
            IdValorClasificacion6 = productBase.CIDVALORCLASIFICACION6;
            Precio = productBase.CPRECIO1;
        }
        public ProductoDto() { }
    }
}
