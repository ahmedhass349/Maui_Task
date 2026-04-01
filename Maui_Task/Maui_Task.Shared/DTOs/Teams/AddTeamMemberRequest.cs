using System.ComponentModel.DataAnnotations;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.DTOs.Teams
{
    public class AddTeamMemberRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public TeamRole Role { get; set; } = TeamRole.Member;
    }
}
