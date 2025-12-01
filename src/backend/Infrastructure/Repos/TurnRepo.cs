using Core.Domain.Entities.Turns;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class TurnRepo : ITurnRepo
    {
        private readonly WeightDBContext _dbContext;
        public TurnRepo(WeightDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Turn> GetTurn(int? weightId = null, string? description = null)
        {
            //get last added turn
            Turn? lastTurn = await _dbContext.Turns
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            Turn newTurn;

            if (lastTurn is not null && lastTurn.CreatedAt.Date == DateTime.UtcNow.Date)
            {
                //same day, increment turn number
                newTurn = new()
                {
                    Number = lastTurn.Number + 1,
                    Description = description ?? $"Turn {lastTurn.Number + 1}",
                    WeightEntryId = weightId
                };
            }
            else
            {
                newTurn = new()
                {
                    Number = 1,
                    Description = description ?? "First Turn of the Day",
                    WeightEntryId = weightId
                };
            }

            await _dbContext.Turns.AddAsync(newTurn);

            await _dbContext.SaveChangesAsync();

            return newTurn;
        }
    }
}
