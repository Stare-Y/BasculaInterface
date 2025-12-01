using Core.Application.Services;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Service
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _client;

        public ApiService(HttpClient client)
        {
            _client = client;
        }

        public string GetBaseUrl()
        {
            return _client.BaseAddress?.ToString() ?? throw new InvalidOperationException("Base address is not set.");
        }

        private async Task LogRequest(HttpRequestMessage? requestMessage = null, string? endpoint = null, StringContent? payload = null)
        {
            try
            {
                if (requestMessage is null)
                {
                    if (endpoint is not null)
                    {
                        Debug.WriteLine("==== HTTP REQUEST ====");
                        Debug.WriteLine($"{endpoint}");
                    }

                    if (payload is not null)
                    {
                        Debug.WriteLine("Request Body:");
                        Debug.WriteLine(await payload.ReadAsStringAsync());
                    }

                    return;
                }

                Debug.WriteLine("==== HTTP REQUEST ====");
                Debug.WriteLine($"{requestMessage?.Method} {requestMessage?.RequestUri}");

                if (requestMessage?.Content != null)
                {
                    string requestBody = await requestMessage.Content.ReadAsStringAsync();
                    Debug.WriteLine("Request Body:");
                    Debug.WriteLine(requestBody);
                }
            }
            catch
            {
                return;
            }
        }

        private async Task LogResponse(HttpResponseMessage response)
        {
            Debug.WriteLine("==== HTTP RESPONSE ====");
            Debug.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    string formattedJson = System.Text.Json.JsonSerializer.Serialize(
                        doc,
                        new JsonSerializerOptions { WriteIndented = true }
                    );

                    Debug.WriteLine("Response Body:");
                    Debug.WriteLine(formattedJson);
                }
                catch
                {
                    // Si no es JSON, lo imprimimos como está
                    Debug.WriteLine("Response Body:");
                    Debug.WriteLine(responseBody);
                }
            }
        }

        public async Task<T> SendAsync<T>(HttpRequestMessage requestMessage)
        {
            await LogRequest(requestMessage);

            HttpResponseMessage response = await _client.SendAsync(requestMessage);

            await LogResponse(response);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            await LogRequest(endpoint: "GET " + endpoint);

            HttpResponseMessage response = await _client.GetAsync(endpoint);

            await LogResponse(response);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            StringContent content = await SerializeContent(data);

            await LogRequest(endpoint: "POST " + endpoint, payload: content);

            HttpResponseMessage response = await _client.PostAsync(endpoint, content);

            await LogResponse(response);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        public async Task<T> PutAsync<T>(string endpoint, object? data)
        {
            StringContent content = await SerializeContent(data);

            await LogRequest(endpoint: "PUT " + endpoint, payload: content);

            HttpResponseMessage response = await _client.PutAsync(endpoint, content);

            await LogResponse(response);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        public async Task<T> PatchAsync<T>(string endpoint, object? data)
        {
            StringContent content = await SerializeContent(data);

            await LogRequest(endpoint: "PATCH " + endpoint, payload: content);

            HttpResponseMessage response = await _client.PatchAsync(endpoint, content);

            await LogResponse(response);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            await LogRequest(endpoint: "DELETE " + endpoint);

            HttpResponseMessage response = await _client.DeleteAsync(endpoint);

            await LogResponse(response);

            await ValidateResponse(response);

            return response.IsSuccessStatusCode;
        }

        private async Task<StringContent> SerializeContent(object? data)
        {
            return await Task.Run(() =>
            {
                string json = JsonSerializer.Serialize(data);
                return new StringContent(json, Encoding.UTF8, "application/json");
            });
        }

        public static async Task ValidateResponse(HttpResponseMessage response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response), "Server response is null");

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Status: {response.StatusCode}, Error: {error}");
            }
        }

        private async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            T? result = JsonSerializer.Deserialize<T>(responseContent, options);
            return result ?? throw new Exception("Hubo un error, la respuesta del servidor resulto nula");
        }
    }
}
