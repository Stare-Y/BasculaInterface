using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IExternalTargetBehaviorService
    {
        Task<IEnumerable<ExternalTargetBehaviorDto>> GetAllAsync();
        Task<ExternalTargetBehaviorDto> GetByIdAsync(int id);
    }
}
