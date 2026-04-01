// FILE: Repositories/TaskCommentRepository.cs
// STATUS: NEW
// CHANGES: Created for fully exposing TaskComment entity (#21)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Web.Repositories.Interfaces;

namespace Maui_Task.Web.Repositories
{
    public class TaskCommentRepository : GenericRepository<TaskComment>, ITaskCommentRepository
    {
        public TaskCommentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TaskComment>> GetByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Include(c => c.Author)
                .Where(c => c.TaskId == taskId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<TaskComment?> GetByIdWithAuthorAsync(int commentId)
        {
            return await _dbSet
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == commentId);
        }
    }
}
