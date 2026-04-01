using System.Threading.Tasks;
using Maui_Task.Shared.Services;
using Microsoft.JSInterop;

namespace Maui_Task.Web.Client.Services
{
    public class WasmTokenStorage : ITokenStorage
    {
        private const string Key = "taskflow_token";
        private const string RefreshKey = "taskflow_refresh";
        private readonly IJSRuntime _js;

        public WasmTokenStorage(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SetTokenAsync(string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                await _js.InvokeVoidAsync("secureStorage.removeItem", Key);
            }
            else
            {
                await _js.InvokeAsync<bool>("secureStorage.setEncryptedItem", Key, token);
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                var v = await _js.InvokeAsync<string?>("secureStorage.getEncryptedItem", Key);
                return string.IsNullOrEmpty(v) ? null : v;
            }
            catch
            {
                return null;
            }
        }

        public Task RemoveTokenAsync()
        {
            return _js.InvokeVoidAsync("secureStorage.removeItem", Key).AsTask();
        }

        public async Task SetRefreshTokenAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                await _js.InvokeVoidAsync("secureStorage.removeItem", RefreshKey);
            }
            else
            {
                await _js.InvokeAsync<bool>("secureStorage.setEncryptedItem", RefreshKey, refreshToken);
            }
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            try
            {
                var v = await _js.InvokeAsync<string?>("secureStorage.getEncryptedItem", RefreshKey);
                return string.IsNullOrEmpty(v) ? null : v;
            }
            catch
            {
                return null;
            }
        }

        public Task RemoveRefreshTokenAsync()
        {
            return _js.InvokeVoidAsync("secureStorage.removeItem", RefreshKey).AsTask();
        }
    }
}
