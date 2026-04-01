using System;

namespace Maui_Task.Shared.Data.Entities
{
    public class CalendarEvent
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string? Color { get; set; }   // hex colour for the event block
        public string? MeetingLink { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser Owner { get; set; } = null!;
    }
}
