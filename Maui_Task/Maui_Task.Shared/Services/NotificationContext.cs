using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Maui_Task.Shared.Services
{
    /// <summary>
    /// Scoped notification state container for real-time updates and UI subscriptions.
    /// </summary>
    public sealed class NotificationContext : INotificationClientService
    {
        private const int MaxCachedNotifications = 50;
        private readonly IAuthService _auth;
        private readonly HttpApiService _api;
        private readonly NavigationManager _navigation;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private HubConnection? _connection;
        private CancellationTokenSource? _pollingCts;
        private bool _isInitialized;
        private bool _isDisposed;

        public List<NotificationDto> Notifications { get; private set; } = new();
        public int UnreadCount { get; private set; }
        public bool IsOnline { get; private set; }

        public event Action<NotificationDto>? OnNotificationReceived;
        public event Action<int>? OnNotificationRead;
        public event Action<int>? OnNotificationDeleted;
        public event Action<int>? OnUnreadCountChanged;
        public event Action? OnStateChanged;

        public NotificationContext(IAuthService auth, HttpApiService api, NavigationManager navigation)
        {
            _auth = auth;
            _api = api;
            _navigation = navigation;

            _auth.OnTokenRefreshed += HandleTokenRefreshed;
        }

        public async Task InitializeAsync()
        {
            if (_isDisposed || _isInitialized)
            {
                return;
            }

            _isInitialized = true;

            await RefreshFromApiAsync();
            _ = ConnectWithBackoffAsync();
        }

        public void AddNotification(NotificationDto notification)
        {
            if (_isDisposed)
            {
                return;
            }

            if (Notifications.Any(n => n.Id == notification.Id))
            {
                return;
            }

            Notifications.Insert(0, notification);
            if (Notifications.Count > MaxCachedNotifications)
            {
                Notifications = Notifications.Take(MaxCachedNotifications).ToList();
            }

            if (!notification.IsRead)
            {
                UpdateUnreadCount(UnreadCount + 1);
            }
            else
            {
                NotifySubscribers();
            }

            OnNotificationReceived?.Invoke(notification);
        }

        public void RemoveNotification(int notificationId)
        {
            if (_isDisposed)
            {
                return;
            }

            var item = Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (item == null)
            {
                return;
            }

            var wasUnread = !item.IsRead;
            Notifications.Remove(item);

            if (wasUnread)
            {
                UpdateUnreadCount(Math.Max(0, UnreadCount - 1));
            }
            else
            {
                NotifySubscribers();
            }

            OnNotificationDeleted?.Invoke(notificationId);
        }

        public void MarkAsRead(int notificationId)
        {
            if (_isDisposed)
            {
                return;
            }

            var item = Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (item == null || item.IsRead)
            {
                return;
            }

            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
            UpdateUnreadCount(Math.Max(0, UnreadCount - 1));

            OnNotificationRead?.Invoke(notificationId);
        }

        public void ClearAll()
        {
            if (_isDisposed)
            {
                return;
            }

            Notifications.Clear();
            UpdateUnreadCount(0);
        }

        public void NotifySubscribers()
        {
            if (_isDisposed)
            {
                return;
            }

            OnStateChanged?.Invoke();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                await _api.PatchAsync<ApiResponse<string>>($"/api/notifications/{notificationId}/read", null);
                MarkAsRead(notificationId);
            }
            catch
            {
                await RefreshFromApiAsync();
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            try
            {
                await _api.PatchAsync<ApiResponse<string>>("/api/notifications/read-all", null);

                foreach (var notification in Notifications.Where(n => !n.IsRead))
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                UpdateUnreadCount(0);
            }
            catch
            {
                await RefreshFromApiAsync();
            }
        }

        private async Task ConnectWithBackoffAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (_isDisposed)
                {
                    return;
                }

                if (_connection == null)
                {
                    _connection = BuildConnection();
                }

                if (_connection.State == HubConnectionState.Connected || _connection.State == HubConnectionState.Connecting)
                {
                    return;
                }

                var attempt = 0;
                while (!_isDisposed)
                {
                    try
                    {
                        await _connection.StartAsync();
                        IsOnline = true;
                        StopPollingFallback();
                        NotifySubscribers();
                        await RefreshUnreadCountAsync();
                        return;
                    }
                    catch
                    {
                        IsOnline = false;
                        NotifySubscribers();
                        StartPollingFallback();

                        attempt++;
                        var delaySeconds = Math.Min(30, (int)Math.Pow(2, Math.Min(attempt, 5)));
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private HubConnection BuildConnection()
        {
            var hubUrl = _navigation.ToAbsoluteUri("/hubs/notifications");
            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_auth.Token);
                })
                .Build();

            connection.On<NotificationDto>("ReceiveNotification", AddNotification);
            connection.On<int>("UnreadCount", UpdateUnreadCount);
            connection.On<int>("NotificationRead", id =>
            {
                MarkAsRead(id);
            });
            connection.On<int>("NotificationDeleted", id =>
            {
                RemoveNotification(id);
            });

            connection.Reconnecting += _ =>
            {
                IsOnline = false;
                NotifySubscribers();
                StartPollingFallback();
                return Task.CompletedTask;
            };

            connection.Reconnected += async _ =>
            {
                IsOnline = true;
                StopPollingFallback();
                NotifySubscribers();
                await RefreshFromApiAsync();
            };

            connection.Closed += async _ =>
            {
                IsOnline = false;
                NotifySubscribers();
                StartPollingFallback();
                await ConnectWithBackoffAsync();
            };

            return connection;
        }

        private async Task RefreshFromApiAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                var notificationsResponse = await _api.GetAsync<ApiResponse<IEnumerable<NotificationDto>>>("/api/notifications?page=1&pageSize=20");
                var unreadResponse = await _api.GetAsync<ApiResponse<int>>("/api/notifications/unread-count");

                Notifications = notificationsResponse?.Data?.OrderByDescending(n => n.CreatedAt).ToList() ?? new List<NotificationDto>();
                UpdateUnreadCount(unreadResponse?.Data ?? Notifications.Count(n => !n.IsRead));
            }
            catch
            {
                NotifySubscribers();
            }
        }

        private async Task RefreshUnreadCountAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                var unreadResponse = await _api.GetAsync<ApiResponse<int>>("/api/notifications/unread-count");
                UpdateUnreadCount(unreadResponse?.Data ?? 0);
            }
            catch
            {
                StartPollingFallback();
            }
        }

        private void UpdateUnreadCount(int newCount)
        {
            var bounded = Math.Max(0, newCount);
            var changed = bounded != UnreadCount;
            UnreadCount = bounded;

            if (changed)
            {
                OnUnreadCountChanged?.Invoke(UnreadCount);
            }

            NotifySubscribers();
        }

        private void StartPollingFallback()
        {
            if (_pollingCts != null || _isDisposed)
            {
                return;
            }

            _pollingCts = new CancellationTokenSource();
            var token = _pollingCts.Token;

            _ = Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
                try
                {
                    while (await timer.WaitForNextTickAsync(token))
                    {
                        await RefreshFromApiAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }, token);
        }

        private void StopPollingFallback()
        {
            if (_pollingCts == null)
            {
                return;
            }

            _pollingCts.Cancel();
            _pollingCts.Dispose();
            _pollingCts = null;
        }

        private void HandleTokenRefreshed()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_connection == null)
                    {
                        return;
                    }

                    if (_connection.State == HubConnectionState.Connected)
                    {
                        await _connection.StopAsync();
                    }

                    await ConnectWithBackoffAsync();
                }
                catch
                {
                    StartPollingFallback();
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _auth.OnTokenRefreshed -= HandleTokenRefreshed;

            StopPollingFallback();

            if (_connection != null)
            {
                try
                {
                    await _connection.StopAsync();
                }
                catch
                {
                }

                try
                {
                    await _connection.DisposeAsync();
                }
                catch
                {
                }
            }

            _gate.Dispose();
        }
    }
}
