namespace Core.Application.DTOs
{
    public class TurnDto
    {
        public int Number { get; set; }
        public string? Description { get; set; }
        public int? WeightEntryId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string PrintData(string? partnerName = null)
        {
            return
    $@"

***** Turno: {Number} *****
{Description ?? ""}
Cliente: 
{partnerName ?? "PUBLICO EN GENERAL"}
Creado: 
{CreatedAt:dd-MM-yyyy HH:mm}
------------------
COOPERATIVA PEDRO EZQUEDA

¡Gracias!

";
        }
    }
}
