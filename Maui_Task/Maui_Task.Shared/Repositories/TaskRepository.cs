using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public TaskRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<TaskItem>> GetAllByUserIdAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.TaskItems
                .AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Where(t => t.AssigneeId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.TaskItems
                .AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TaskItem> CreateAsync(TaskItem task)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.TaskItems.Add(task);
            await db.SaveChangesAsync();
            return task;
        }

        public async Task<TaskItem> UpdateAsync(TaskItem task)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.TaskItems.Update(task);
            await db.SaveChangesAsync();
            return task;
        }

        public async Task DeleteAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var task = await db.TaskItems.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return;
            }

            db.TaskItems.Remove(task);
            await db.SaveChangesAsync();
        }

        public async Task<List<TaskItem>> GetDueSoonAsync(int userId, DateTime from, DateTime to)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.TaskItems
                .AsNoTracking()
                .Where(t => t.AssigneeId == userId
                            && t.DueDate.HasValue
                            && t.DueDate.Value >= from
                            && t.DueDate.Value <= to
                            && t.Status != Data.Entities.TaskStatus.Completed)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetOverdueAsync(int userId)
        {
            var now = DateTime.UtcNow;
            await using var db = await _factory.CreateDbContextAsync();
            return await db.TaskItems
                .AsNoTracking()
                .Where(t => t.AssigneeId == userId
                            && t.DueDate.HasValue
                            && t.DueDate.Value < now
                            && t.Status != Data.Entities.TaskStatus.Completed)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetByStatusAsync(int userId, string status)
        {
            await using var db = await _factory.CreateDbContextAsync();
            if (!Enum.TryParse<Data.Entities.TaskStatus>(status, true, out var parsed))
            {
                return new List<TaskItem>();
            }

            return await db.TaskItems
                .AsNoTracking()
                .Where(t => t.AssigneeId == userId && t.Status == parsed)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetByPriorityAsync(int userId, string priority)
        {
            await using var db = await _factory.CreateDbContextAsync();
            if (!Enum.TryParse<TaskPriority>(priority, true, out var parsed))
            {
                return new List<TaskItem>();
            }

            return await db.TaskItems
                .AsNoTracking()
                .Where(t => t.AssigneeId == userId && t.Priority == parsed)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetByCourseAsync(int userId, string course)
        {
            var normalized = course.Trim().ToLowerInvariant();
            await using var db = await _factory.CreateDbContextAsync();
            return await db.TaskItems
                .AsNoTracking()
                .Include(t => t.Project)
                .Where(t => t.AssigneeId == userId
                            && t.Project != null
                            && t.Project.Name.ToLower().Contains(normalized))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
