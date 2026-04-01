using System.Threading.Tasks;
using Maui_Task.Shared.Services;
using Microsoft.Maui.Storage;

namespace Maui_Task.Services
{
    public class MauiSecureTokenStorage : ITokenStorage
    {
        private const string Key = "taskflow_token";
        private const string RefreshKey = "taskflow_refresh";

        public async Task SetTokenAsync(string? token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    try
                    {
                        SecureStorage.Remove(Key);
                    }
                    catch
                    {
                        await SecureStorage.SetAsync(Key, string.Empty);
                    }
                }
                else
                {
                    await SecureStorage.SetAsync(Key, token);
                }
            }
            catch
            {
                // ignore
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                var v = await SecureStorage.GetAsync(Key);
                return string.IsNullOrEmpty(v) ? null : v;
            }
            catch
            {
                return null;
            }
        }

        public Task RemoveTokenAsync()
        {
            try
            {
                SecureStorage.Remove(Key);
            }
            catch
            {
                // ignore
            }
            return Task.CompletedTask;
        }

        public async Task SetRefreshTokenAsync(string? refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    try { SecureStorage.Remove(RefreshKey); }
                    catch { await SecureStorage.SetAsync(RefreshKey, string.Empty); }
                }
                else
                {
                    await SecureStorage.SetAsync(RefreshKey, refreshToken);
                }
            }
            catch { }
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            try
            {
                var v = await SecureStorage.GetAsync(RefreshKey);
                return string.IsNullOrEmpty(v) ? null : v;
            }
            catch
            {
                return null;
            }
        }

        public Task RemoveRefreshTokenAsync()
        {
            try { SecureStorage.Remove(RefreshKey); } catch { }
            return Task.CompletedTask;
        }
    }
}
