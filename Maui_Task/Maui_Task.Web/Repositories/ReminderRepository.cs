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
    public class ReminderRepository : GenericRepository<Reminder>, IReminderRepository
    {
        public ReminderRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Reminder>> GetPendingAsync(DateTime upTo)
        {
            return await _dbSet
                .Where(r => !r.HasFired && r.FireAt <= upTo)
                .Include(r => r.Task)
                .Include(r => r.User)
                .OrderBy(r => r.FireAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reminder>> GetByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(r => r.TaskId == taskId)
                .OrderBy(r => r.FireAt)
                .ToListAsync();
        }

        public async Task<Reminder> CreateAsync(Reminder reminder)
        {
            await _dbSet.AddAsync(reminder);
            await _context.SaveChangesAsync();
            return reminder;
        }

        public async Task CreateBatchAsync(IEnumerable<Reminder> reminders)
        {
            await _dbSet.AddRangeAsync(reminders);
            await _context.SaveChangesAsync();
        }

        public async Task MarkFiredAsync(int reminderId)
        {
            var reminder = await _dbSet.FindAsync(reminderId);
            if (reminder != null)
            {
                reminder.HasFired = true;
                reminder.FiredAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByTaskIdAsync(int taskId)
        {
            var reminders = await _dbSet
                .Where(r => r.TaskId == taskId)
                .ToListAsync();

            _dbSet.RemoveRange(reminders);
            await _context.SaveChangesAsync();
        }
    }
}
