using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Settings;
using Maui_Task.Shared.DTOs.Auth;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface ISettingsService
    {
        Task<ProfileDto> GetProfileAsync(int userId);
        Task<AuthResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
        Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task DeleteAccountAsync(int userId);
    }
}
