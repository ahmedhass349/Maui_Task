using System;

namespace Maui_Task.Shared.Data.Entities
{
    public enum NotificationType
    {
        TaskCreated,
        TaskUpdated,
        TaskDeleted,
        TaskDueSoon,
        TaskOverdue,
        TaskCompleted,
        ReminderFired,
        AccountWelcome,
        AccountProfileUpdated,
        SystemAnnouncement
    }

    public enum NotificationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; }
        public bool IsRead { get; set; } = false;
        public string? ActionUrl { get; set; }
        public int? RelatedTaskId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation
        public AppUser User { get; set; } = null!;
        public TaskItem? RelatedTask { get; set; }
    }
}
