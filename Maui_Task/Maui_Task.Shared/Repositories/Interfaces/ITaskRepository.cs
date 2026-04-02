using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.Repositories.Interfaces
{
    public interface ITaskRepository
    {
        Task<List<TaskItem>> GetAllByUserIdAsync(int userId);
        Task<TaskItem?> GetByIdAsync(int id);
        Task<TaskItem> CreateAsync(TaskItem task);
        Task<TaskItem> UpdateAsync(TaskItem task);
        Task DeleteAsync(int id);
        Task<List<TaskItem>> GetDueSoonAsync(int userId, DateTime from, DateTime to);
        Task<List<TaskItem>> GetOverdueAsync(int userId);
        Task<List<TaskItem>> GetByStatusAsync(int userId, string status);
        Task<List<TaskItem>> GetByPriorityAsync(int userId, string priority);
        Task<List<TaskItem>> GetByCourseAsync(int userId, string course);
    }
}
