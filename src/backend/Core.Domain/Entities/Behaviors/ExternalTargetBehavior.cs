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
    }
}
