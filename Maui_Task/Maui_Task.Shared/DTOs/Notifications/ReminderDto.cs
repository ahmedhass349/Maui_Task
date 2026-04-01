using System;

namespace Maui_Task.Shared.DTOs.Notifications
{
    public class ReminderDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public DateTime FireAt { get; set; }
        public bool NotifyEmail { get; set; }
        public bool NotifyInApp { get; set; }
        public bool HasFired { get; set; }
    }

    public class CreateReminderDto
    {
        public int TaskId { get; set; }
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> ReminderMap { get; set; } = new();
        // key = "YYYY-MM-DD", value = ["9:00 AM", "2:00 PM"]
        public bool NotifyEmail { get; set; }
        public bool NotifyInApp { get; set; }
    }
}
