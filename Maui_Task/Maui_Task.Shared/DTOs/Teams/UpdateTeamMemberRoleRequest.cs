using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.DTOs.Teams
{
    public class UpdateTeamMemberRoleRequest
    {
        public TeamRole Role { get; set; } = TeamRole.Member;
    }
}
