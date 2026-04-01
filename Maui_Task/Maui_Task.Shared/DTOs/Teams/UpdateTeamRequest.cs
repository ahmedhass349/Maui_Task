// FILE: DTOs/Teams/UpdateTeamRequest.cs
// STATUS: NEW
// CHANGES: Added for missing UpdateTeam endpoint (#22)

namespace Maui_Task.Shared.DTOs.Teams
{
    public class UpdateTeamRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
