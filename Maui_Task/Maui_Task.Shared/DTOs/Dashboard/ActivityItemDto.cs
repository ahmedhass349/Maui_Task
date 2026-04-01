using System;

namespace Maui_Task.Shared.DTOs.Dashboard
{
    public class ActivityItemDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
