using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Tasks;

namespace Maui_Task.Shared.Services
{
    public interface ITaskService
    {
        Task<List<TaskDto>> GetTasksAsync();
        Task<TaskDto?> GetTaskAsync(int id);
        Task<TaskDto?> SaveTaskAsync(CreateTaskRequest request, int? taskId = null);
        Task<TaskDto?> ToggleStarAsync(int taskId);
        Task<TaskDto?> UpdateStatusAsync(int taskId, string status);
        Task DeleteTaskAsync(int taskId);
    }
}