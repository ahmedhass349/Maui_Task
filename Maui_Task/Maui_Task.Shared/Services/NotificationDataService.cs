using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Services
{
    public class NotificationDataService : INotificationDataService
    {
        private readonly HttpApiService _api;
        private readonly AppDbContext _db;
        private readonly TaskFlowAuthStateProvider _authState;
        private readonly ISyncQueueService _syncQueue;

        public NotificationDataService(HttpApiService api, AppDbContext db, TaskFlowAuthStateProvider authState, ISyncQueueService syncQueue)
        {
            _api = api;
            _db = db;
            _authState = authState;
            _syncQueue = syncQueue;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync()
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<IEnumerable<NotificationDto>>>("/api/notifications");
                if (response?.Data != null)
                {
                    return response.Data.OrderByDescending(n => n.CreatedAt).ToList();
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var userId = await ResolveCurrentUserIdAsync();
            return await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    Priority = n.Priority.ToString(),
                    IsRead = n.IsRead,
                    ActionUrl = n.ActionUrl,
                    RelatedTaskId = n.RelatedTaskId,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt,
                    TimeAgo = string.Empty
                })
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            var notifications = await GetNotificationsAsync();
            return notifications.Count(n => !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            try
            {
                await _api.PatchAsync<ApiResponse<string>>($"/api/notifications/{id}/read", null);
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var local = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            if (local is null)
            {
                return false;
            }

            local.IsRead = true;
            local.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Notification", "read", new NotificationIdSyncPayload(id));
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync()
        {
            try
            {
                await _api.PatchAsync<ApiResponse<string>>("/api/notifications/read-all", null);
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var userId = await ResolveCurrentUserIdAsync();
            var items = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var notification in items)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Notification", "read-all", new { });
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                await _api.DeleteAsync($"/api/notifications/{id}");
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var local = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            if (local is null)
            {
                return false;
            }

            _db.Notifications.Remove(local);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Notification", "delete", new NotificationIdSyncPayload(id));
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
            try
            {
                await _api.DeleteAsync("/api/notifications");
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var userId = await ResolveCurrentUserIdAsync();
            var local = await _db.Notifications.Where(n => n.UserId == userId).ToListAsync();
            if (local.Count == 0)
            {
                return false;
            }

            _db.Notifications.RemoveRange(local);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Notification", "delete-all", new { });
            return true;
        }

        private async Task<int> ResolveCurrentUserIdAsync()
        {
            var currentUserId = _authState.CurrentUser?.Id ?? 0;
            if (currentUserId > 0)
            {
                return currentUserId;
            }

            var localUser = await _db.AppUsers.OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt).FirstOrDefaultAsync();
            return localUser?.Id ?? 0;
        }
    }
}