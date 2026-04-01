using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Web.Repositories.Interfaces
{
    public interface IProjectRepository : IGenericRepository<Project>
    {
        Task<IEnumerable<Project>> GetUserProjectsAsync(int userId);
        Task<Project?> GetProjectWithDetailsAsync(int projectId);
    }
}
