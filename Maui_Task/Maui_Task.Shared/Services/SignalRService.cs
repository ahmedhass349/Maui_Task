/*
    CLEANUP [Maui_Task.Shared/Services/SignalRService.cs]:
    - Removed: Orphaned hub listener for ReceiveNotification that had no matching server SendAsync call.
    - Fixed: Aligned SignalR client handler with NotificationHub canonical method name UnreadCount.
    - Moved: none
*/

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Maui_Task.Shared.Services
{
    public class SignalRService
    {
        private readonly string _hubUrl = "/hubs/notifications";
        private readonly AuthenticationService _auth;
        private HubConnection? _connection;

        public event Action<string>? OnNotificationReceived;

        public SignalRService(AuthenticationService auth)
        {
            _auth = auth;
            // refresh SignalR connection when auth token is rotated
            _auth.OnTokenRefreshed += () => { _ = RefreshAsync(); };
        }

        public async Task StartAsync(string baseUrl)
        {
            if (_connection != null) return;

            var url = new Uri(new Uri(baseUrl), _hubUrl).ToString();
            _connection = new HubConnectionBuilder()
                .WithUrl(url, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_auth.Token);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<int>("UnreadCount", (count) =>
            {
                OnNotificationReceived?.Invoke(count.ToString());
            });

            _connection.Reconnecting += async (ex) =>
            {
                // optionally notify UI
                await Task.CompletedTask;
            };

            _connection.Reconnected += async (id) =>
            {
                // reconnected; nothing special to do because AccessTokenProvider is used on negotiation
                await Task.CompletedTask;
            };

            _connection.Closed += async (ex) =>
            {
                // try to restart after a delay
                await Task.Delay(2000);
                try
                {
                    await _connection!.StartAsync();
                }
                catch
                {
                    // ignore - will rely on automatic reconnect
                }
            };

            await _connection.StartAsync();
        }

        public async Task RefreshAsync()
        {
            if (_connection == null) return;
            try
            {
                await _connection.StopAsync();
                await _connection.StartAsync();
            }
            catch
            {
                // ignore
            }
        }

        public async Task StopAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}
