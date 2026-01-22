using Core.Domain.Entities.ContpaqiSQL;
using System.Diagnostics.CodeAnalysis;

namespace Core.Application.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int IdValorClasificacion6 { get; set; }
        public double Precio { get; set; }

        [SetsRequiredMembers]
        public ProductoDto(Producto productBase)
        {
            Id = productBase.CIDPRODUCTO;
            Nombre = productBase.CNOMBREPRODUCTO;
            IdValorClasificacion6 = productBase.CIDVALORCLASIFICACION6;
            Precio = productBase.CPRECIO1;
            Code = productBase.CCODIGOPRODUCTO;
        }
        public ProductoDto() { }
    }
}
