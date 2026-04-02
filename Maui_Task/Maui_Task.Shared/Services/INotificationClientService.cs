using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Notifications;

namespace Maui_Task.Shared.Services
{
    public interface INotificationClientService : IAsyncDisposable
    {
        List<NotificationDto> Notifications { get; }
        int UnreadCount { get; }
        bool IsOnline { get; }

        event Action<NotificationDto>? OnNotificationReceived;
        event Action<int>? OnNotificationRead;
        event Action<int>? OnNotificationDeleted;
        event Action<int>? OnUnreadCountChanged;
        event Action? OnStateChanged;

        Task InitializeAsync();
        void AddNotification(NotificationDto notification);
        void RemoveNotification(int notificationId);
        void MarkAsRead(int notificationId);
        void ClearAll();
        void NotifySubscribers();
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync();
    }
}