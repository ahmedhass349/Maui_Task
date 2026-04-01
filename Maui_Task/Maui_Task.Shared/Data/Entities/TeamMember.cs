namespace Maui_Task.Shared.Data.Entities
{
    public enum TeamRole { Member, Admin }

    public class TeamMember
    {
        public int TeamId { get; set; }
        public int UserId { get; set; }
        public TeamRole Role { get; set; } = TeamRole.Member;

        // Navigation
        public Team Team { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}
