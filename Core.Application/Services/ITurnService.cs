using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface ITurnService
    {
        Task<TurnDto> GetTurn(int? weightId = null, string? description = null);
    }
}
