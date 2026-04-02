using System;

namespace Maui_Task.Shared.DTOs.Teams
{
    public class TeamMemberCardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "offline";
        public int TasksCompleted { get; set; }
        public int TasksInProgress { get; set; }
        public string Initials { get; set; } = string.Empty;
        public DateTime? LastActiveAt { get; set; }
    }
}