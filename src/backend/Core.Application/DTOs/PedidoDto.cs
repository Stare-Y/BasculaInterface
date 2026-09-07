using Core.Domain.Entities.ProviderOrders;

namespace Core.Application.DTOs
{
    public class PedidoDto
    {
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public DateTime ExpectedArrival { get; set; }
        public string? Notes { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? CreatedAt { get; set; }
        public ICollection<PedidoLineDto> Lines { get; set; } = [];

        /// <summary>
        /// True only once every line is concluded (naturally or force-closed).
        /// A pedido with no lines is never considered concluded. Computed — no
        /// stored header-level flag (design.md Decision 4).
        /// </summary>
        public bool Concluded => Lines.Count > 0 && Lines.All(l => l.Concluded);

        public PedidoDto() { }

        public PedidoDto(Pedido entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            Id = entity.Id;
            ProviderId = entity.ProviderId;
            ExpectedArrival = entity.ExpectedArrival;
            Notes = entity.Notes;
            LastUpdated = entity.LastUpdated;
            CreatedAt = entity.CreatedAt;
            Lines = [.. entity.Lines
                .Where(l => !l.IsDeleted)
                .Select(l => new PedidoLineDto(l))];
        }

        public Pedido ToEntity()
        {
            return new Pedido
            {
                Id = Id,
                ProviderId = ProviderId,
                ExpectedArrival = ExpectedArrival,
                Notes = Notes,
                LastUpdated = LastUpdated,
                Lines = [.. Lines.Select(l => l.ToEntity())]
            };
        }
    }
}
