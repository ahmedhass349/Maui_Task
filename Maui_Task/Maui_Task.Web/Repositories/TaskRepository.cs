using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Web.Repositories.Interfaces;

namespace Maui_Task.Web.Repositories
{
    public class TaskRepository : GenericRepository<TaskItem>, ITaskRepository
    {
        public TaskRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TaskItem>> GetFilteredTasksAsync(
            int userId, string? status, string? priority, int? projectId, bool? starred)
        {
            IQueryable<TaskItem> query = _dbSet
                .AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Where(t => t.AssigneeId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<Maui_Task.Shared.Data.Entities.TaskStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    query = query.Where(t => t.Status == parsedStatus);
                }
            }

            if (!string.IsNullOrEmpty(priority))
            {
                if (Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsedPriority))
                {
                    query = query.Where(t => t.Priority == parsedPriority);
                }
            }

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            if (starred.HasValue)
            {
                query = query.Where(t => t.IsStarred == starred.Value);
            }

            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetByProjectIdAsync(int projectId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.Comments)
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
