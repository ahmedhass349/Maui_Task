using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Maui_Task.Shared.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _client;
        private readonly ITokenStorage _storage;
        private string? _token;

        public AuthenticationService(HttpClient client, ITokenStorage storage)
        {
            _client = client;
            _storage = storage;
        }

        public string? Token => _token;

        public ClaimsPrincipal CreatePrincipalFromToken(string? token = null)
        {
            var value = string.IsNullOrWhiteSpace(token) ? _token : token;
            if (string.IsNullOrWhiteSpace(value))
                return new ClaimsPrincipal(new ClaimsIdentity());

            var identity = new ClaimsIdentity(ParseClaimsFromJwt(value), "jwt");
            return new ClaimsPrincipal(identity);
        }

        private void ApplyTokenHeader(string? token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _client.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                var stored = await _storage.GetTokenAsync();
                if (!string.IsNullOrEmpty(stored))
                {
                    _token = stored;
                    ApplyTokenHeader(_token);
                }
                else
                {
                    // try to refresh using a stored refresh token
                    var refresh = await _storage.GetRefreshTokenAsync();
                    if (!string.IsNullOrEmpty(refresh))
                    {
                        await TryRefreshTokenAsync();
                    }
                }
            }
            catch
            {
                // ignore storage errors during init
            }
        }

        public event System.Action? OnTokenRefreshed;

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
                if (string.IsNullOrEmpty(refreshToken))
                    await _storage.RemoveRefreshTokenAsync();
                else
                    await _storage.SetRefreshTokenAsync(refreshToken);
            }
            catch
            {
                // ignore
            }
        }

        private System.Threading.CancellationTokenSource? _refreshCts;

        private void StartRefreshLoop(System.DateTime? refreshExpiry)
        {
            try
            {
                _refreshCts?.Cancel();
            }
            catch { }

            if (!refreshExpiry.HasValue) return;

            var delay = refreshExpiry.Value - System.DateTime.UtcNow - System.TimeSpan.FromMinutes(1);
            if (delay < System.TimeSpan.Zero) delay = System.TimeSpan.Zero;

            _refreshCts = new System.Threading.CancellationTokenSource();
            var token = _refreshCts.Token;

            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(delay, token);
                    while (!token.IsCancellationRequested)
                    {
                        var ok = await TryRefreshTokenAsync();
                        if (!ok)
                        {
                            // failed refresh: back off and retry
                            await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(30), token);
                            continue;
                        }

                        // after successful refresh, compute next delay from stored expiry
                        var newExpiry = await _storage.GetRefreshTokenAsync();
                        // If server returned expiry in response, TryRefreshTokenAsync will have stored tokens; rely on next loop to compute.
                        // Wait until shortly before expiry again; to be conservative, try again in 50% of remaining time.
                        await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromMinutes(10), token);
                    }
                }
                catch (System.OperationCanceledException) { }
                catch { }
            }, token);
        }

        public void SetToken(string? token)
        {
            _token = token;
            ApplyTokenHeader(token);
        }

        public async Task SetTokenAsync(string? token)
        {
            SetToken(token);
            if (string.IsNullOrEmpty(token))
            {
                await _storage.RemoveTokenAsync();
            }
            else
            {
                await _storage.SetTokenAsync(token);
            }
        }

        public async Task<T?> LoginAsync<T>(string path, object credentials)
        {
            var resp = await _client.PostAsJsonAsync(path, credentials);
            resp.EnsureSuccessStatusCode();
            var obj = await resp.Content.ReadFromJsonAsync<T?>();
            return obj;
        }

        public async Task<bool> TryRefreshTokenAsync()
        {
            var refresh = await GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refresh)) return false;

            try
            {
                var resp = await _client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refresh });
                resp.EnsureSuccessStatusCode();
                var auth = await resp.Content.ReadFromJsonAsync<Maui_Task.Shared.DTOs.Auth.AuthResponse?>();
                if (auth == null || string.IsNullOrEmpty(auth.Token)) return false;

                await SetTokenAsync(auth.Token);
                if (!string.IsNullOrEmpty(auth.RefreshToken))
                    await SetRefreshTokenAsync(auth.RefreshToken);

                // start refresh background loop using expiry if provided
                StartRefreshLoop(auth.RefreshTokenExpiry);

                OnTokenRefreshed?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _client.PostAsync("/api/auth/logout", null);
            }
            catch
            {
                // ignore
            }

            await SetTokenAsync(null);
            await SetRefreshTokenAsync(null);
        }

        private static System.Collections.Generic.IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new System.Collections.Generic.List<Claim>();
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return claims;

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
            var json = System.Text.Encoding.UTF8.GetString(bytes);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
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

                if (element.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var val in element.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(claimType, val.GetString() ?? string.Empty));
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
