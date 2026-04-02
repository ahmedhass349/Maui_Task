using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Settings;
using Maui_Task.Shared.Repositories.Interfaces;
using Maui_Task.Shared.Utilities;

namespace Maui_Task.Shared.Services
{
    public class LocalSettingsDataService : ISettingsDataService
    {
        private readonly IUserRepository _users;
        private readonly IAuthService _auth;
        private readonly TaskFlowAuthStateProvider _authState;

        public LocalSettingsDataService(IUserRepository users, IAuthService auth, TaskFlowAuthStateProvider authState)
        {
            _users = users;
            _auth = auth;
            _authState = authState;
        }

        public async Task<ProfileDto?> GetProfileAsync()
        {
            var user = await LocalUserResolver.ResolveCurrentUserAsync(_auth, _users);
            return user == null ? null : MapProfile(user);
        }

        public async Task<AuthResponse?> SaveProfileAsync(UpdateProfileRequest request)
        {
            var user = await LocalUserResolver.ResolveCurrentUserAsync(_auth, _users);
            if (user == null)
            {
                return null;
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.FullName = $"{user.FirstName} {user.LastName}".Trim();
            user.Email = request.Email?.Trim() ?? user.Email;
            user.AvatarUrl = request.AvatarUrl;
            user.Company = request.Company;
            user.Country = request.Country;
            user.Phone = request.Phone;
            user.Timezone = request.Timezone;
            user.RefreshToken = LocalTokenGenerator.Generate();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            await _users.UpdateAsync(user);

            var response = BuildAuthResponse(user);
            await _auth.SetTokenAsync(response.Token);
            if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                await _auth.SetRefreshTokenAsync(response.RefreshToken);
            }

            _authState.NotifyUserLoggedIn(response);
            return response;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            var user = await LocalUserResolver.ResolveCurrentUserAsync(_auth, _users);
            if (user == null)
            {
                return false;
            }

            if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return false;
            }

            var password = PasswordHasher.HashPassword(request.NewPassword);
            user.PasswordHash = password.hash;
            user.PasswordSalt = password.salt;
            await _users.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteAccountAsync()
        {
            var user = await LocalUserResolver.ResolveCurrentUserAsync(_auth, _users);
            if (user == null)
            {
                return false;
            }

            await _users.DeleteAsync(user.Id);
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

        private static AuthResponse BuildAuthResponse(AppUser user)
        {
            var dto = MapProfile(user);
            var refreshToken = string.IsNullOrWhiteSpace(user.RefreshToken) ? LocalTokenGenerator.Generate() : user.RefreshToken;
            var refreshExpiry = user.RefreshTokenExpiry ?? DateTime.UtcNow.AddDays(30);
            var token = BuildToken(dto, user.Role);

            return new AuthResponse
            {
                Token = token,
                User = dto,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshExpiry
            };
        }

        private static string BuildToken(UserDto user, string? role)
        {
            var headerJson = JsonSerializer.Serialize(new { alg = "none", typ = "JWT" });
            var payloadJson = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["sub"] = user.Id.ToString(),
                ["unique_name"] = user.FullName,
                ["email"] = user.Email,
                ["role"] = string.IsNullOrWhiteSpace(role) ? "User" : role,
                ["exp"] = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds(),
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["iss"] = "taskflow-local"
            });

            return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson))}.{Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson))}.";
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}