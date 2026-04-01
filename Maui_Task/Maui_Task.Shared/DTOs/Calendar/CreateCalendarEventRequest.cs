using System;
using System.ComponentModel.DataAnnotations;

namespace Maui_Task.Shared.DTOs.Calendar
{
    public class CreateCalendarEventRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }

        public string? Color { get; set; }
        public string? MeetingLink { get; set; }
    }
}
