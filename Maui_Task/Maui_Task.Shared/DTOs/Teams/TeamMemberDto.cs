// FILE: DTOs/Teams/TeamMemberDto.cs
// STATUS: UPDATED
// CHANGES: Added Initials field for frontend avatar compatibility (#30)

namespace Maui_Task.Shared.DTOs.Teams
{
    public class TeamMemberDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;       // Computed: e.g. "JD" for "John Doe" (#30)
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public int TasksCompleted { get; set; }
        public int TasksInProgress { get; set; }
    }
}
