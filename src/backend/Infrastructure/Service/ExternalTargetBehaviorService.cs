using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class ExternalTargetBehaviorService : IExternalTargetBehaviorService
    {
        private readonly IExternalTargetBehaviorRepo _externalTargetBehaviorRepo;
        public ExternalTargetBehaviorService(IExternalTargetBehaviorRepo externalTargetBehaviorRepo)
        {
            _externalTargetBehaviorRepo = externalTargetBehaviorRepo;
        }
        public async Task<IEnumerable<ExternalTargetBehaviorDto>> GetAllAsync()
        {
            return (await _externalTargetBehaviorRepo.GetAllAsync()).Select(behavior => new ExternalTargetBehaviorDto(behavior));
        }
        public async Task<ExternalTargetBehaviorDto> GetByIdAsync(int id)
        {
            return new ExternalTargetBehaviorDto(await _externalTargetBehaviorRepo.GetByIdAsync(id));
        }
    }
}
