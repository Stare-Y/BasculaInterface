using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Turns;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class TurnService : ITurnService
    {
        private readonly ITurnRepo _turnRepo;
        public TurnService(ITurnRepo turnRepo)
        {
            _turnRepo = turnRepo;
        }

        public async Task<TurnDto> GetTurn(int? weightId = null, string? description = null)
        {
            Turn turn =  await _turnRepo.GetTurn(weightId, description);

            return new()
            {
                Number = turn.Number,
                Description = turn.Description,
                WeightEntryId = turn.WeightEntryId,
                CreatedAt = turn.CreatedAt
            };
        }
    }
}
