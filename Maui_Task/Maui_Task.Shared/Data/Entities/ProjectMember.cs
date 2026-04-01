namespace Maui_Task.Shared.Data.Entities
{
    public enum ProjectRole { Member, Admin }

    public class ProjectMember
    {
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public ProjectRole Role { get; set; } = ProjectRole.Member;

        // Navigation
        public Project Project { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}
