using System.Net.Http.Json;

namespace BasculaTerminalTest.Integration
{
    internal static class HttpAssert
    {
        /// <summary>
        /// Like <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> but puts the response
        /// body in the failure message — the API wraps most errors in a 400 with the real reason
        /// in the JSON, which the default exception hides.
        /// </summary>
        public static async Task<HttpResponseMessage> EnsureOk(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Xunit.Sdk.XunitException(
                    $"Expected success but got {(int)response.StatusCode} {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}\n{body}");
            }

            return response;
        }

        public static async Task<T> ReadAs<T>(this HttpResponseMessage response)
        {
            await response.EnsureOk();
            return (await response.Content.ReadFromJsonAsync<T>())
                ?? throw new Xunit.Sdk.XunitException($"Response body deserialized to null for {typeof(T).Name}");
        }
    }
}
