using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Projects;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<ProjectDto>> GetUserProjectsAsync(int userId);
        Task<ProjectDto> GetProjectByIdAsync(int projectId);
        Task<ProjectDto> CreateProjectAsync(int userId, CreateProjectRequest request);
        Task<ProjectDto> UpdateProjectAsync(int userId, int projectId, UpdateProjectRequest request);
        Task DeleteProjectAsync(int userId, int projectId);
        Task<ProjectDto> ToggleStarAsync(int userId, int projectId);
        Task<IEnumerable<ProjectMemberDto>> GetProjectMembersAsync(int projectId);
    }
}
