using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Teams;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface ITeamService
    {
        Task<IEnumerable<TeamDto>> GetUserTeamsAsync(int userId);
        Task<TeamDto> CreateTeamAsync(int userId, CreateTeamRequest request);
        Task<TeamDto> UpdateTeamAsync(int userId, int teamId, UpdateTeamRequest request);
        Task DeleteTeamAsync(int userId, int teamId);
        Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int teamId);
        Task AddTeamMemberAsync(int teamId, AddTeamMemberRequest request);
        Task RemoveTeamMemberAsync(int userId, int teamId, int memberUserId);
        Task UpdateTeamMemberRoleAsync(int userId, int teamId, int memberUserId, Maui_Task.Shared.Data.Entities.TeamRole role);
    }
}
