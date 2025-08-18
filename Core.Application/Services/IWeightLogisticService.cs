namespace Core.Application.Services
{
    public interface IWeightLogisticService
    {
        Task<bool> RequestWeight(string deviceId);
        Task<bool> ReleaseWeight(string deviceId);
    }
}
