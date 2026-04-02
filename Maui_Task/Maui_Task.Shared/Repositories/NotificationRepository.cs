using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public NotificationRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<Notification>> GetByUserIdAsync(int userId, int page, int pageSize)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            return notification;
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification == null)
            {
                return;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var unread = await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var item in unread)
            {
                item.IsRead = true;
                item.ReadAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int notificationId, int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification == null)
            {
                return;
            }

            db.Notifications.Remove(notification);
            await db.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUnreadAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
}
