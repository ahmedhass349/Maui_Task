using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Auth;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<string> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task<UserDto> GetCurrentUserAsync(int userId);
        Task<AuthResponse> RefreshAsync(string refreshToken);
        Task LogoutAsync(int userId);
    }
}
