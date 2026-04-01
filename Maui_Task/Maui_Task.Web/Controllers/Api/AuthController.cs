using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/AuthController.cs
// STATUS: MODIFIED
// CHANGES: Fixed GetUserId() to return 401 (#3), removed try-catch blocks (#15), cleaned usings (#17)

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// Handles authentication operations including login, registration, and password management.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Extracts the authenticated user's ID from the JWT claims.
        /// Throws <see cref="UnauthorizedAccessException"/> if the claim is missing.
        /// </summary>
        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Authenticates a user with email and password credentials.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            var secure = Request.IsHttps;

            Response.Cookies.Append("taskflow_token", result.Token, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(24)
            });

            // Set refresh token as HttpOnly cookie for browser clients
            if (!string.IsNullOrEmpty(result.RefreshToken) && result.RefreshTokenExpiry.HasValue)
            {
                Response.Cookies.Append("taskflow_refresh", result.RefreshToken, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = secure,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                    Expires = result.RefreshTokenExpiry
                });
            }

            return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful"));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var result = await _authService.RefreshAsync(request.RefreshToken);
            var secure = Request.IsHttps;

            Response.Cookies.Append("taskflow_token", result.Token, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(24)
            });

            if (!string.IsNullOrEmpty(result.RefreshToken) && result.RefreshTokenExpiry.HasValue)
            {
                Response.Cookies.Append("taskflow_refresh", result.RefreshToken, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = secure,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                    Expires = result.RefreshTokenExpiry
                });
            }

            return Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed"));
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            var secure = Request.IsHttps;
            Response.Cookies.Append("taskflow_token", result.Token, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(24)
            });

            return StatusCode(201, ApiResponse<AuthResponse>.Ok(result, "Registration successful"));
        }

        /// <summary>
        /// Initiates a password reset flow by sending an email with a reset token.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var code = await _authService.ForgotPasswordAsync(request);
            return Ok(ApiResponse<string>.Ok(code, "Recovery code generated"));
        }

        /// <summary>
        /// Resets a user's password using a previously issued reset token.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(ApiResponse<string>.Ok("Password has been reset", "Password reset successful"));
        }

        /// <summary>
        /// Retrieves the currently authenticated user's profile information.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetUserId();
            var user = await _authService.GetCurrentUserAsync(userId);
            return Ok(ApiResponse<UserDto>.Ok(user));
        }

        /// <summary>
        /// Logs out the current user. Since JWT is stateless, this endpoint simply acknowledges the logout request.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            try
            {
                var id = GetUserId();
                _authService.LogoutAsync(id).GetAwaiter().GetResult();
            }
            catch
            {
                // ignore
            }

            Response.Cookies.Delete("taskflow_token");
            Response.Cookies.Delete("taskflow_refresh");

            return Ok(ApiResponse<string>.Ok("Logged out", "Logout successful"));
        }
    }
}
