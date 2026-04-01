using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Web.Repositories.Interfaces
{
    public interface ITaskRepository : IGenericRepository<TaskItem>
    {
        Task<IEnumerable<TaskItem>> GetFilteredTasksAsync(int userId, string? status, string? priority, int? projectId, bool? starred);
        Task<IEnumerable<TaskItem>> GetByProjectIdAsync(int projectId);
    }
}
