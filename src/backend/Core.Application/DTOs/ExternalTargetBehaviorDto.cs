using Core.Domain.Entities.Behaviors;

namespace Core.Application.DTOs
{
    public class ExternalTargetBehaviorDto
    {
        public int Id { get; set; }
        public string? TargetSerie { get; set; }
        public string? TargetName { get; set; }
        public string? TargetAlmacen { get; set; }
        public string? AlmacenName { get; set; }
        public bool Hidden { get; set; }

        public string DisplayText => TargetSerie is null ? TargetName ?? "Unknown" : $"{TargetSerie} - {TargetName}";

        /// <summary>Label for the pedido almacén picker: "{code} - {name}", falling back to the code alone.</summary>
        public string AlmacenDisplayText => string.IsNullOrWhiteSpace(AlmacenName)
            ? TargetAlmacen ?? "Sin almacén"
            : $"{TargetAlmacen} - {AlmacenName}";

        public ExternalTargetBehaviorDto(ExternalTargetBehavior externalTargetBehavior)
        {
            if (externalTargetBehavior == null)
            {
                throw new ArgumentNullException(nameof(externalTargetBehavior), "ExternalTargetBehavior cannot be null");
            }
            Id = externalTargetBehavior.Id;
            TargetSerie = externalTargetBehavior.TargetSerie;
            TargetName = externalTargetBehavior.TargetName;
            TargetAlmacen = externalTargetBehavior.TargetAlmacen;
            AlmacenName = externalTargetBehavior.AlmacenName;
            Hidden = externalTargetBehavior.Hidden;
        }
        public ExternalTargetBehaviorDto() { }
    }
}
