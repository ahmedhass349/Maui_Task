using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Maui_Task.Shared.Services
{
    public interface IAuthService
    {
        string? Token { get; }
        event Action? OnTokenRefreshed;

        ClaimsPrincipal CreatePrincipalFromToken(string? token = null);
        Task InitializeAsync(bool allowOnlineRefresh = true);
        Task<string?> GetRefreshTokenAsync();
        Task SetRefreshTokenAsync(string? refreshToken);
        void SetToken(string? token);
        Task SetTokenAsync(string? token);
        Task<T?> LoginAsync<T>(string path, object credentials);
        Task<bool> TryRefreshTokenAsync();
        Task LogoutAsync();
    }
}