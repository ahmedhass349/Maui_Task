using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Settings;

namespace Maui_Task.Shared.Services
{
    public interface ISettingsDataService
    {
        Task<ProfileDto?> GetProfileAsync();
        Task<AuthResponse?> SaveProfileAsync(UpdateProfileRequest request);
        Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
        Task<bool> DeleteAccountAsync();
    }
}