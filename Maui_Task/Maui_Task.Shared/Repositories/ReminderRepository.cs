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
    public class ReminderRepository : IReminderRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ReminderRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<Reminder>> GetPendingAsync(DateTime upTo)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Reminders
                .AsNoTracking()
                .Include(r => r.Task)
                .Where(r => !r.HasFired && r.FireAt <= upTo)
                .OrderBy(r => r.FireAt)
                .ToListAsync();
        }

        public async Task<List<Reminder>> GetByTaskIdAsync(int taskId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Reminders
                .AsNoTracking()
                .Where(r => r.TaskId == taskId)
                .OrderBy(r => r.FireAt)
                .ToListAsync();
        }

        public async Task<Reminder> CreateAsync(Reminder reminder)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.Reminders.Add(reminder);
            await db.SaveChangesAsync();
            return reminder;
        }

        public async Task CreateBatchAsync(IEnumerable<Reminder> reminders)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.Reminders.AddRange(reminders);
            await db.SaveChangesAsync();
        }

        public async Task MarkFiredAsync(int reminderId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var reminder = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId);
            if (reminder == null)
            {
                return;
            }

            reminder.HasFired = true;
            reminder.FiredAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task DeleteByTaskIdAsync(int taskId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var reminders = await db.Reminders.Where(r => r.TaskId == taskId).ToListAsync();
            if (reminders.Count == 0)
            {
                return;
            }

            db.Reminders.RemoveRange(reminders);
            await db.SaveChangesAsync();
        }
    }
}
