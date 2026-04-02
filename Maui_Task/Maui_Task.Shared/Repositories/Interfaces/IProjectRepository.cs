using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.Repositories.Interfaces
{
    public interface IProjectRepository
    {
        Task<List<Project>> GetAllByUserIdAsync(int userId);
        Task<Project?> GetByIdAsync(int id);
        Task<Project> CreateAsync(Project project);
        Task<Project> UpdateAsync(Project project);
        Task DeleteAsync(int id);
    }
}
