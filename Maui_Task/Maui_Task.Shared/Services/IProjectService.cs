using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Projects;

namespace Maui_Task.Shared.Services
{
    public interface IProjectService
    {
        Task<List<ProjectDto>> GetProjectsAsync();
        Task<ProjectDto?> GetProjectAsync(int id);
        Task<ProjectDto?> SaveProjectAsync(CreateProjectRequest request, int? projectId = null);
        Task DeleteProjectAsync(int id);
    }
}