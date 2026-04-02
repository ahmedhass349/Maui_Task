using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.Repositories.Interfaces;

namespace Maui_Task.Shared.Services
{
    internal static class LocalUserResolver
    {
        public static async Task<int> ResolveCurrentUserIdAsync(IAuthService auth, IUserRepository users)
        {
            var principal = auth.CreatePrincipalFromToken();
            var idValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
            if (int.TryParse(idValue, out var currentUserId) && currentUserId > 0)
            {
                return currentUserId;
            }

            var fallback = await users.GetAllAsync();
            return fallback.FirstOrDefault()?.Id ?? 0;
        }

        public static async Task<AppUser?> ResolveCurrentUserAsync(IAuthService auth, IUserRepository users)
        {
            var currentUserId = await ResolveCurrentUserIdAsync(auth, users);
            if (currentUserId <= 0)
            {
                return null;
            }

            return await users.GetByIdAsync(currentUserId);
        }
    }
}