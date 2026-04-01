/*
    CLEANUP [Maui_Task.Shared/Services/TaskFlowAuthStateProvider.cs]:
    - Removed: none
    - Fixed: Added deterministic event unsubscription to prevent long-lived OnTokenRefreshed subscription leaks.
    - Moved: none
*/

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Maui_Task.Shared.DTOs.Auth;

namespace Maui_Task.Shared.Services
{
    /// <summary>
    /// Custom AuthenticationStateProvider that bridges the JWT-based
    /// AuthenticationService with Blazor's AuthorizeView / CascadingAuthState.
    /// Stored user info is parsed from the current in-memory token.
    /// </summary>
    public class TaskFlowAuthStateProvider : AuthenticationStateProvider, IDisposable
    {
        private readonly AuthenticationService _auth;
        private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
        private readonly Action _tokenRefreshedHandler;

        /// <summary>Currently authenticated user DTO (null when anonymous).</summary>
        public UserDto? CurrentUser { get; private set; }

        public TaskFlowAuthStateProvider(AuthenticationService auth)
        {
            _auth = auth;
            // Re-notify Blazor whenever the token is refreshed silently
            _tokenRefreshedHandler = () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            _auth.OnTokenRefreshed += _tokenRefreshedHandler;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var user = _auth.CreatePrincipalFromToken();
                if (user.Identity?.IsAuthenticated != true)
                    return Task.FromResult(new AuthenticationState(_anonymous));

                return Task.FromResult(new AuthenticationState(user));
            }
            catch
            {
                return Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        /// <summary>Call after a successful login to push new state to all Blazor components.</summary>
        public void NotifyUserLoggedIn(AuthResponse response)
        {
            CurrentUser = response.User;
            _auth.SetToken(response.Token);

            try
            {
                var user = _auth.CreatePrincipalFromToken(response.Token);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
            }
            catch
            {
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
            }
        }

        /// <summary>Call on logout to clear state everywhere.</summary>
        public void NotifyUserLoggedOut()
        {
            CurrentUser = null;
            _auth.SetToken(null);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }

        public void Dispose()
        {
            _auth.OnTokenRefreshed -= _tokenRefreshedHandler;
        }
    }
}
