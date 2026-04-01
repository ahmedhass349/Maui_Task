using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface INotificationService
    {
        // Core creation
        Task<NotificationDto> CreateAsync(int userId, string title, string message,
            NotificationType type, NotificationPriority priority,
            string? actionUrl = null, int? relatedTaskId = null);

        // Convenience methods per event
        Task NotifyTaskCreatedAsync(int userId, TaskItem task);
        Task NotifyTaskUpdatedAsync(int userId, TaskItem task, string whatChanged);
        Task NotifyTaskDeletedAsync(int userId, string taskTitle);
        Task NotifyTaskDueSoonAsync(int userId, TaskItem task, string timeframe);
        Task NotifyTaskOverdueAsync(int userId, TaskItem task);
        Task NotifyTaskCompletedAsync(int userId, TaskItem task);
        Task NotifyReminderFiredAsync(int userId, TaskItem task, DateTime fireTime);
        Task NotifyWelcomeAsync(int userId, string firstName);
        Task NotifyProfileUpdatedAsync(int userId);

        // Query
        Task<IEnumerable<NotificationDto>> GetForUserAsync(int userId, int page, int pageSize);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAsync(int notificationId, int userId);
        Task DeleteAllAsync(int userId);
        Task<NotificationDto?> GetByIdAsync(int notificationId, int userId);
    }
}
