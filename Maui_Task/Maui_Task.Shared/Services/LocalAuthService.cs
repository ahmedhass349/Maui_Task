using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.Helpers;
using Maui_Task.Shared.Repositories.Interfaces;
using Maui_Task.Shared.Utilities;

namespace Maui_Task.Shared.Services
{
    public class LocalAuthService : IAuthService
    {
        private readonly ITokenStorage _storage;
        private readonly IUserRepository _users;
        private string? _token;

        public LocalAuthService(ITokenStorage storage, IUserRepository users)
        {
            _storage = storage;
            _users = users;
        }

        public string? Token => _token;

        public event Action? OnTokenRefreshed;

        public ClaimsPrincipal CreatePrincipalFromToken(string? token = null)
        {
            var value = string.IsNullOrWhiteSpace(token) ? _token : token;
            if (string.IsNullOrWhiteSpace(value))
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            var identity = new ClaimsIdentity(ParseClaimsFromJwt(value), "local-jwt");
            return new ClaimsPrincipal(identity);
        }

        public async Task InitializeAsync(bool allowOnlineRefresh = true)
        {
            try
            {
                var stored = await _storage.GetTokenAsync();
                if (!string.IsNullOrWhiteSpace(stored))
                {
                    _token = stored;
                }
            }
            catch
            {
            }
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            try
            {
                return await _storage.GetRefreshTokenAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task SetRefreshTokenAsync(string? refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    await _storage.RemoveRefreshTokenAsync();
                }
                else
                {
                    await _storage.SetRefreshTokenAsync(refreshToken);
                }
            }
            catch
            {
            }
        }

        public void SetToken(string? token)
        {
            _token = token;
        }

        public async Task SetTokenAsync(string? token)
        {
            SetToken(token);
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    await _storage.RemoveTokenAsync();
                }
                else
                {
                    await _storage.SetTokenAsync(token);
                }
            }
            catch
            {
            }
        }

        public async Task<T?> LoginAsync<T>(string path, object credentials)
        {
            var normalizedPath = path.Trim().ToLowerInvariant();

            if (normalizedPath.Contains("/api/auth/login"))
            {
                var login = credentials as LoginRequest ?? ToModel<LoginRequest>(credentials);
                if (login == null)
                {
                    return default;
                }

                var response = await SignInAsync(login.Email, login.Password);
                return ConvertResponse<T>(response);
            }

            if (normalizedPath.Contains("/api/auth/register"))
            {
                var register = credentials as RegisterRequest ?? ToModel<RegisterRequest>(credentials);
                if (register == null)
                {
                    return default;
                }

                var response = await RegisterAsync(register);
                return ConvertResponse<T>(response);
            }

            if (normalizedPath.Contains("/api/auth/forgot-password"))
            {
                var request = credentials as ForgotPasswordRequest ?? ToModel<ForgotPasswordRequest>(credentials);
                if (request == null)
                {
                    return default;
                }

                var response = await ForgotPasswordAsync(request);
                return ConvertResponse<T>(response);
            }

            if (normalizedPath.Contains("/api/auth/reset-password"))
            {
                var request = credentials as ResetPasswordRequest ?? ToModel<ResetPasswordRequest>(credentials);
                if (request == null)
                {
                    return default;
                }

                var response = await ResetPasswordAsync(request);
                return ConvertResponse<T>(response);
            }

            return default;
        }

        public async Task<bool> TryRefreshTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_token))
            {
                return true;
            }

            var refresh = await GetRefreshTokenAsync();
            return !string.IsNullOrWhiteSpace(refresh);
        }

        public async Task LogoutAsync()
        {
            await SetTokenAsync(null);
            await SetRefreshTokenAsync(null);
        }

        private async Task<AuthResponse?> SignInAsync(string email, string password)
        {
            var user = await _users.GetByEmailAsync(email);
            if (user == null)
            {
                return null;
            }

            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            await UpdateLoginStateAsync(user);
            return BuildAuthResponse(user);
        }

        private async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            if (await _users.EmailExistsAsync(request.Email))
            {
                return ApiResponse<AuthResponse>.Fail("An account with that email already exists.");
            }

            var nameParts = SplitName(request.FullName);
            var password = PasswordHasher.HashPassword(request.Password);
            var user = new AppUser
            {
                FirstName = nameParts.FirstName,
                LastName = nameParts.LastName,
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Username = BuildUsername(request.Email, request.FullName),
                Role = "User",
                PasswordHash = password.hash,
                PasswordSalt = password.salt,
                Company = request.Company,
                Country = request.Country,
                Phone = request.Phone,
                Timezone = request.Timezone,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            await _users.CreateAsync(user);
            var response = await UpdateLoginStateAsync(user);
            return ApiResponse<AuthResponse>.Ok(response!, "Registration successful.");
        }

        private async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _users.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return ApiResponse<string>.Ok(string.Empty, "If the email exists, a reset token has been generated.");
            }

            user.ResetToken = LocalTokenGenerator.Generate();
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(2);
            await _users.UpdateAsync(user);
            return ApiResponse<string>.Ok(user.ResetToken, "Password reset token generated locally.");
        }

        private async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _users.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return ApiResponse<string>.Fail("Invalid reset token or email.");
            }

            if (string.IsNullOrWhiteSpace(user.ResetToken)
                || user.ResetToken != request.Token
                || !user.ResetTokenExpiry.HasValue
                || user.ResetTokenExpiry.Value < DateTime.UtcNow)
            {
                return ApiResponse<string>.Fail("Invalid or expired reset token.");
            }

            var password = PasswordHasher.HashPassword(request.NewPassword);
            user.PasswordHash = password.hash;
            user.PasswordSalt = password.salt;
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _users.UpdateAsync(user);
            return ApiResponse<string>.Ok(string.Empty, "Password updated locally.");
        }

        private async Task<AuthResponse> UpdateLoginStateAsync(AppUser user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.RefreshToken = LocalTokenGenerator.Generate();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
            await _users.UpdateAsync(user);

            var response = BuildAuthResponse(user);
            await SetTokenAsync(response.Token);
            await SetRefreshTokenAsync(response.RefreshToken);
            OnTokenRefreshed?.Invoke();
            return response;
        }

        private AuthResponse BuildAuthResponse(AppUser user)
        {
            var dto = MapUser(user);
            return new AuthResponse
            {
                Token = BuildToken(dto, user.Role),
                User = dto,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiry = user.RefreshTokenExpiry
            };
        }

        private static UserDto MapUser(AppUser user)
        {
            return new UserDto
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

        private static T? ConvertResponse<T>(object? value)
        {
            if (value == null)
            {
                return default;
            }

            if (value is T typed)
            {
                return typed;
            }

            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
        }

        private static TModel? ToModel<TModel>(object value)
        {
            return JsonSerializer.Deserialize<TModel>(JsonSerializer.Serialize(value));
        }

        private static (string FirstName, string LastName) SplitName(string fullName)
        {
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return ("Offline", "User");
            }

            if (parts.Length == 1)
            {
                return (parts[0], "User");
            }

            return (parts[0], string.Join(' ', parts.Skip(1)));
        }

        private static string BuildUsername(string email, string fullName)
        {
            var candidate = email.Split('@')[0];
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }

            return fullName.Trim().Replace(' ', '.').ToLowerInvariant();
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                return claims;
            }

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2:
                    payload += "==";
                    break;
                case 3:
                    payload += "=";
                    break;
            }

            var bytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(bytes);

            using var doc = JsonDocument.Parse(json);
            foreach (var element in doc.RootElement.EnumerateObject())
            {
                var claimType = element.Name switch
                {
                    "sub" => ClaimTypes.NameIdentifier,
                    "email" => ClaimTypes.Email,
                    "unique_name" => ClaimTypes.Name,
                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" => ClaimTypes.Role,
                    "role" => ClaimTypes.Role,
                    _ => element.Name
                };

                if (element.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(claimType, item.GetString() ?? string.Empty));
                    }
                }
                else
                {
                    claims.Add(new Claim(claimType, element.Value.GetString() ?? string.Empty));
                }
            }

            return claims;
        }
    }
}