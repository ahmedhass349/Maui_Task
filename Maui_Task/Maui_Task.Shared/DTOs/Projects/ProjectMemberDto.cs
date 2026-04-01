namespace Maui_Task.Shared.DTOs.Projects
{
    public class ProjectMemberDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;   // "Member" or "Admin"
    }
}
