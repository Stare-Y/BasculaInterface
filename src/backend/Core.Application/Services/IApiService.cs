namespace Core.Application.Services
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
        Task<T> SendAsync<T>(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default);
        Task<T> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);
        Task<T> PutAsync<T>(string endpoint, object? data, CancellationToken cancellationToken = default);
        Task<T> PatchAsync<T>(string endpoint, object? data, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
        string GetBaseUrl();
    }
}
