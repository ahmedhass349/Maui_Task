using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Repositories.Interfaces;

namespace Maui_Task.Shared.Services
{
    public class LocalNotificationClientService : INotificationClientService
    {
        private const int MaxCachedNotifications = 50;
        private readonly INotificationRepository _notifications;
        private readonly IAuthService _auth;
        private readonly IUserRepository _users;

        public LocalNotificationClientService(INotificationRepository notifications, IAuthService auth, IUserRepository users)
        {
            _notifications = notifications;
            _auth = auth;
            _users = users;
        }

        public List<NotificationDto> Notifications { get; private set; } = new();
        public int UnreadCount { get; private set; }
        public bool IsOnline { get; private set; } = true;

        public event Action<NotificationDto>? OnNotificationReceived;
        public event Action<int>? OnNotificationRead;
        public event Action<int>? OnNotificationDeleted;
        public event Action<int>? OnUnreadCountChanged;
        public event Action? OnStateChanged;

        public async Task InitializeAsync()
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                Notifications = new List<NotificationDto>();
                UpdateUnreadCount(0);
                return;
            }

            var items = await _notifications.GetByUserIdAsync(userId, 1, MaxCachedNotifications);
            Notifications = items
                .Select(Map)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            UpdateUnreadCount(Notifications.Count(n => !n.IsRead));
        }

        public void AddNotification(NotificationDto notification)
        {
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
            var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification == null)
            {
                return;
            }

            var wasUnread = !notification.IsRead;
            Notifications.Remove(notification);

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
            var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification == null || notification.IsRead)
            {
                return;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            UpdateUnreadCount(Math.Max(0, UnreadCount - 1));
            OnNotificationRead?.Invoke(notificationId);
        }

        public void ClearAll()
        {
            Notifications.Clear();
            UpdateUnreadCount(0);
        }

        public void NotifySubscribers()
        {
            OnStateChanged?.Invoke();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return;
            }

            await _notifications.MarkAsReadAsync(notificationId, userId);
            MarkAsRead(notificationId);
        }

        public async Task MarkAllAsReadAsync()
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return;
            }

            await _notifications.MarkAllAsReadAsync(userId);
            foreach (var item in Notifications.Where(n => !n.IsRead))
            {
                item.IsRead = true;
                item.ReadAt = DateTime.UtcNow;
            }

            UpdateUnreadCount(0);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
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

        private static NotificationDto Map(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                Priority = notification.Priority.ToString(),
                IsRead = notification.IsRead,
                ActionUrl = notification.ActionUrl,
                RelatedTaskId = notification.RelatedTaskId,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt,
                TimeAgo = string.Empty
            };
        }
    }
}