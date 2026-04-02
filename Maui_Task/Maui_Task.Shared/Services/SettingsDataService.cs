using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Settings;
using Maui_Task.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Services
{
    public class SettingsDataService : ISettingsDataService
    {
        private readonly HttpApiService _api;
        private readonly AppDbContext _db;
        private readonly IAuthService _auth;
        private readonly TaskFlowAuthStateProvider _authState;
        private readonly ISyncQueueService _syncQueue;

        public SettingsDataService(HttpApiService api, AppDbContext db, IAuthService auth, TaskFlowAuthStateProvider authState, ISyncQueueService syncQueue)
        {
            _api = api;
            _db = db;
            _auth = auth;
            _authState = authState;
            _syncQueue = syncQueue;
        }

        public async Task<ProfileDto?> GetProfileAsync()
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<ProfileDto>>("/api/settings/profile");
                if (response?.Data != null)
                {
                    return response.Data;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var user = await LoadLocalUserAsync();
            return user is null ? null : MapProfile(user);
        }

        public async Task<AuthResponse?> SaveProfileAsync(UpdateProfileRequest request)
        {
            try
            {
                var response = await _api.PutAsync<ApiResponse<AuthResponse>>("/api/settings/profile", request);
                if (response?.Success == true)
                {
                    if (!string.IsNullOrWhiteSpace(response.Data?.Token))
                    {
                        await _auth.SetTokenAsync(response.Data.Token);
                        _authState.NotifyUserLoggedIn(response.Data);
                    }

                    return response.Data;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var user = await LoadOrCreateLocalUserAsync();
            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.FullName = $"{user.FirstName} {user.LastName}".Trim();
            user.Email = request.Email?.Trim() ?? user.Email;
            user.AvatarUrl = request.AvatarUrl;
            user.Company = request.Company;
            user.Country = request.Country;
            user.Phone = request.Phone;
            user.Timezone = request.Timezone;
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Settings", "profile", new SettingsProfileSyncPayload(request));

            return null;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var response = await _api.PutAsync<ApiResponse<string>>("/api/settings/password", request);
                if (response?.Success == true)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var user = await LoadOrCreateLocalUserAsync();
            user.PasswordHash = request.NewPassword;
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Settings", "password", new SettingsPasswordSyncPayload(request));
            return true;
        }

        public async Task<bool> DeleteAccountAsync()
        {
            try
            {
                await _api.DeleteAsync("/api/settings/account");
                return true;
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var user = await LoadLocalUserAsync();
            if (user is null)
            {
                return false;
            }

            _db.AppUsers.Remove(user);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Settings", "delete-account", new { });
            return true;
        }

        private static ProfileDto MapProfile(AppUser user)
        {
            return new ProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Company = user.Company,
                Country = user.Country,
                Phone = user.Phone,
                Timezone = user.Timezone,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        private async Task<AppUser?> LoadLocalUserAsync()
        {
            var currentUserId = _authState.CurrentUser?.Id ?? 0;
            if (currentUserId > 0)
            {
                var current = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == currentUserId);
                if (current != null)
                {
                    return current;
                }
            }

            return await _db.AppUsers.OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt).FirstOrDefaultAsync();
        }

        private async Task<AppUser> LoadOrCreateLocalUserAsync()
        {
            var user = await LoadLocalUserAsync();
            if (user != null)
            {
                return user;
            }

            user = new AppUser
            {
                FirstName = "Offline",
                LastName = "User",
                FullName = "Offline User",
                Email = $"offline-{Guid.NewGuid():N}@local.taskflow",
                PasswordHash = string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }
    }
}