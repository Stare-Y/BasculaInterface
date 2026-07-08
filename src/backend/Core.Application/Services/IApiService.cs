using System.Net;

namespace Core.Application.Services
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
        Task<T> SendAsync<T>(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default);
        Task<T> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);
        Task<T> PutAsync<T>(string endpoint, object? data, CancellationToken cancellationToken = default);
        Task<T> PatchAsync<T>(string endpoint, object? data, CancellationToken cancellationToken = default);
        Task PatchAsync(string enpoint, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
        /// <summary>
        /// Puts to <paramref name="endpoint"/> and silently retries on 409 Conflict.
        /// On conflict: waits 300ms, calls <paramref name="refetch"/> for fresh state,
        /// calls <paramref name="rebuildBody"/> to produce the new payload, retries up to <paramref name="maxRetries"/> times.
        /// </summary>
        Task<T> PutWithRetryAsync<T>(string endpoint, Func<Task<object?>> buildBody, Func<Task> refetch, int maxRetries = 3);
        string GetBaseUrl();
    }
}
