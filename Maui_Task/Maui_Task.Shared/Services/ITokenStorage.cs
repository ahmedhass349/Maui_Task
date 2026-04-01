using System.Threading.Tasks;

namespace Maui_Task.Shared.Services
{
    public interface ITokenStorage
    {
        Task SetTokenAsync(string? token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();

        Task SetRefreshTokenAsync(string? refreshToken);
        Task<string?> GetRefreshTokenAsync();
        Task RemoveRefreshTokenAsync();
    }
}
