using Core.Domain.Entities.Base;

namespace Core.Domain.Entities.Behaviors
{
    public class ExternalTargetBehavior : BaseEntity
    {
        public string? TargetSerie { get; set; }
        public string? TargetName { get; set; }
        public string? TargetConcept { get; set; }
        public string? TargetDomain { get; set; }
        public string? TargetAlmacen { get; set; }

        /// <summary>
        /// Human-readable name for <see cref="TargetAlmacen"/>, shown in the pedido
        /// convert-to-weight almacén picker as "<c>{TargetAlmacen} - {AlmacenName}</c>".
        /// Only meaningful on hidden behaviors used as almacén targets (design.md Decision 5).
        /// </summary>
        public string? AlmacenName { get; set; }

        public bool Hidden { get; set; } = false;
    }
}
