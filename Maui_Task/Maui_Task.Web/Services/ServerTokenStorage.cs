using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Maui_Task.Shared.Services;

namespace Maui_Task.Web.Services
{
    public class ServerTokenStorage : ITokenStorage
    {
        private const string Key = "taskflow_token";
        private const string RefreshKey = "taskflow_refresh";
        private readonly IHttpContextAccessor _ctx;

        public ServerTokenStorage(IHttpContextAccessor ctx)
        {
            _ctx = ctx;
        }

        public Task SetTokenAsync(string? token)
        {
            var resp = _ctx.HttpContext?.Response;
            var req = _ctx.HttpContext?.Request;
            if (resp != null)
            {
                if (string.IsNullOrEmpty(token))
                {
                    resp.Cookies.Delete(Key);
                }
                else
                {
                    resp.Cookies.Append(Key, token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = req?.IsHttps == true,
                        SameSite = SameSiteMode.Lax
                    });
                }
            }
            return Task.CompletedTask;
        }

        public Task<string?> GetTokenAsync()
        {
            var req = _ctx.HttpContext?.Request;
            var val = req?.Cookies[Key];
            return Task.FromResult<string?>(string.IsNullOrEmpty(val) ? null : val);
        }

        public Task RemoveTokenAsync()
        {
            var resp = _ctx.HttpContext?.Response;
            resp?.Cookies.Delete(Key);
            return Task.CompletedTask;
        }

        public Task SetRefreshTokenAsync(string? refreshToken)
        {
            var resp = _ctx.HttpContext?.Response;
            var req = _ctx.HttpContext?.Request;
            if (resp != null)
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    resp.Cookies.Delete(RefreshKey);
                }
                else
                {
                    resp.Cookies.Append(RefreshKey, refreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = req?.IsHttps == true,
                        SameSite = SameSiteMode.Lax
                    });
                }
            }
            return Task.CompletedTask;
        }

        public Task<string?> GetRefreshTokenAsync()
        {
            var req = _ctx.HttpContext?.Request;
            var val = req?.Cookies[RefreshKey];
            return Task.FromResult<string?>(string.IsNullOrEmpty(val) ? null : val);
        }

        public Task RemoveRefreshTokenAsync()
        {
            var resp = _ctx.HttpContext?.Response;
            resp?.Cookies.Delete(RefreshKey);
            return Task.CompletedTask;
        }
    }
}
