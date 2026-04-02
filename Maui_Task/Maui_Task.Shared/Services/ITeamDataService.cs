using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Teams;

namespace Maui_Task.Shared.Services
{
    public interface ITeamDataService
    {
        Task<List<TeamDto>> GetTeamsAsync();
        Task<List<TeamMemberCardDto>> GetTeamMembersAsync();
        Task<List<UserDto>> GetUsersAsync();
        Task<TeamDto?> GetTeamAsync(int id);
        Task<TeamDto?> SaveTeamAsync(int id, TeamDto team);
    }
}