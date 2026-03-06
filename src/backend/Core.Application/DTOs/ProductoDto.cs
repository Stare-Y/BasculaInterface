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
        public bool IsGranel => IdValorClasificacion6 < 9;
        public double Precio { get; set; }

        public string? IdAlmacen { get; set; } = null;

        [SetsRequiredMembers]
        public ProductoDto(Producto productBase)
        {
            Id = productBase.CIDPRODUCTO;
            Nombre = productBase.CNOMBREPRODUCTO;
            IdValorClasificacion6 = productBase.CIDVALORCLASIFICACION6;
            Precio = productBase.CPRECIO1;
            Code = productBase.CCODIGOPRODUCTO;
            if(productBase.CIDVALORCLASIFICACION6 == 9)
            {
                IdAlmacen = "1";
            }
            else if(productBase.CIDVALORCLASIFICACION6 == 8)
            {
                IdAlmacen = "3";
            }
            else if(productBase.CIDVALORCLASIFICACION6 == 7)
            {
                IdAlmacen = "2";
            }
        }
        public ProductoDto() { }
    }
}
