using Core.Domain.Entities.Behaviors;

namespace Core.Application.DTOs
{
    public class ExternalTargetBehaviorDto
    {
        public int Id { get; set; }
        public string? TargetSerie { get; set; }
        public string? TargetName { get; set; }
        public string DisplayText => $"{TargetSerie} - {TargetName}";
        public ExternalTargetBehaviorDto(ExternalTargetBehavior externalTargetBehavior)
        {
            if (externalTargetBehavior == null)
            {
                throw new ArgumentNullException(nameof(externalTargetBehavior), "ExternalTargetBehavior cannot be null");
            }
            Id = externalTargetBehavior.Id;
            TargetSerie = externalTargetBehavior.TargetSerie;
            TargetName = externalTargetBehavior.TargetName;
        }
        public ExternalTargetBehaviorDto() { }
    }
}
