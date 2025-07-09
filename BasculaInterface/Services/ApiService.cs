using Newtonsoft.Json;
using System.Text;

namespace BasculaInterface.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;

        public ApiService(HttpClient client)
        {
            _client = client;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            HttpResponseMessage response = await _client.GetAsync(endpoint);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            StringContent content = await SerializeContent(data);

            HttpResponseMessage response = await _client.PostAsync(endpoint, content);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        public async Task<T> PutAsync<T>(string endpoint, object? data)
        {
            StringContent content = await SerializeContent(data);

            HttpResponseMessage response = await _client.PutAsync(endpoint, content);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        public async Task<T> PatchAsync<T>(string endpoint, object data)
        {
            StringContent content = await SerializeContent(data);

            HttpResponseMessage response = await _client.PatchAsync(endpoint, content);

            await ValidateResponse(response);

            return await DeserializeResponse<T>(response);
        }

        private async Task<StringContent> SerializeContent(object? data)
        {
            return await Task.Run(() =>
            {
                string json = JsonConvert.SerializeObject(data);
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
            T? result = JsonConvert.DeserializeObject<T>(responseContent);
            return result ?? throw new Exception("Hubo un error, la respuesta del servidor resulto nula");
        }
    }
}
