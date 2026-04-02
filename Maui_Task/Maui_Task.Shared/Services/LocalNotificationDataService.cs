using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Repositories.Interfaces;

namespace Maui_Task.Shared.Services
{
    public class LocalNotificationDataService : INotificationDataService
    {
        private readonly INotificationRepository _notifications;
        private readonly IUserRepository _users;
        private readonly IAuthService _auth;
        private readonly INotificationClientService _notificationClient;

        public LocalNotificationDataService(
            INotificationRepository notifications,
            IUserRepository users,
            IAuthService auth,
            INotificationClientService notificationClient)
        {
            _notifications = notifications;
            _users = users;
            _auth = auth;
            _notificationClient = notificationClient;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync()
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return new List<NotificationDto>();
            }

            var items = await _notifications.GetByUserIdAsync(userId, 1, 50);
            return items.Select(Map).OrderByDescending(n => n.CreatedAt).ToList();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            return userId <= 0 ? 0 : await _notifications.GetUnreadCountAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return false;
            }

            await _notifications.MarkAsReadAsync(id, userId);
            _notificationClient.MarkAsRead(id);
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync()
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return false;
            }

            await _notifications.MarkAllAsReadAsync(userId);
            _notificationClient.ClearAll();
            await _notificationClient.InitializeAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return false;
            }

            await _notifications.DeleteAsync(id, userId);
            _notificationClient.RemoveNotification(id);
            return true;
        }

        public async Task<bool> DeleteManyAsync(IEnumerable<int> ids)
        {
            var changed = false;
            foreach (var id in ids.Distinct().ToList())
            {
                changed |= await DeleteAsync(id);
            }

            return changed;
        }

        public async Task<bool> DeleteAllAsync()
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return false;
            }

            var items = await _notifications.GetByUserIdAsync(userId, 1, 1000);
            if (items.Count == 0)
            {
                return false;
            }

            foreach (var notification in items)
            {
                await _notifications.DeleteAsync(notification.Id, userId);
            }

            _notificationClient.ClearAll();
            return true;
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