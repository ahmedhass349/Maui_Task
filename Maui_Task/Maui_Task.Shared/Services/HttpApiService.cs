using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Maui_Task.Shared.Services
{
    public class HttpApiService
    {
        private readonly HttpClient _client;
        private readonly AuthenticationService _auth;

        public HttpApiService(HttpClient client, AuthenticationService auth)
        {
            _client = client;
            _auth = auth;
        }

        public async Task<T?> GetAsync<T>(string uri)
        {
            var resp = await _client.GetAsync(uri);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var ok = await _auth.TryRefreshTokenAsync();
                if (ok)
                {
                    resp = await _client.GetAsync(uri);
                }
            }
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<T?>();
        }

        public async Task<T?> PostAsync<T>(string uri, object? payload)
        {
            var resp = await _client.PostAsJsonAsync(uri, payload);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var ok = await _auth.TryRefreshTokenAsync();
                if (ok)
                {
                    resp = await _client.PostAsJsonAsync(uri, payload);
                }
            }
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<T?>();
        }

        public async Task<T?> PutAsync<T>(string uri, object? payload)
        {
            var resp = await _client.PutAsJsonAsync(uri, payload);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var ok = await _auth.TryRefreshTokenAsync();
                if (ok)
                {
                    resp = await _client.PutAsJsonAsync(uri, payload);
                }
            }
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<T?>();
        }

        public async Task DeleteAsync(string uri)
        {
            var resp = await _client.DeleteAsync(uri);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var ok = await _auth.TryRefreshTokenAsync();
                if (ok)
                {
                    resp = await _client.DeleteAsync(uri);
                }
            }
            resp.EnsureSuccessStatusCode();
        }

        public async Task<T?> PatchAsync<T>(string uri, object? payload)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, uri)
            {
                Content = payload == null
                    ? null
                    : new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var resp = await _client.SendAsync(request);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var ok = await _auth.TryRefreshTokenAsync();
                if (ok)
                {
                    request = new HttpRequestMessage(HttpMethod.Patch, uri)
                    {
                        Content = payload == null
                            ? null
                            : new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                    };
                    resp = await _client.SendAsync(request);
                }
            }

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<T?>();
        }
    }
}
