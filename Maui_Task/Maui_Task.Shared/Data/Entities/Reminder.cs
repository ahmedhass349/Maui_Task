using System;

namespace Maui_Task.Shared.Data.Entities
{
    public class Reminder
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public DateTime FireAt { get; set; }
        public bool NotifyEmail { get; set; }
        public bool NotifyInApp { get; set; }
        public bool HasFired { get; set; } = false;
        public DateTime? FiredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
