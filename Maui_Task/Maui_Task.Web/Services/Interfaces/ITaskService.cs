using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Tasks;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskDto>> GetTasksAsync(int userId, TaskFilterRequest filter);
        Task<TaskDto> GetTaskByIdAsync(int taskId);
        Task<TaskDto> CreateTaskAsync(int userId, CreateTaskRequest request);
        Task<TaskDto> UpdateTaskAsync(int userId, int taskId, UpdateTaskRequest request);
        Task DeleteTaskAsync(int userId, int taskId);
        Task<TaskDto> ToggleStarAsync(int userId, int taskId);
        Task<TaskDto> UpdateStatusAsync(int userId, int taskId, string status);
    }
}
