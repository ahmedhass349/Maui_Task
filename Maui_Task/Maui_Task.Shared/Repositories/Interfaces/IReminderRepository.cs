using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.Repositories.Interfaces
{
    public interface IReminderRepository
    {
        Task<List<Reminder>> GetPendingAsync(DateTime upTo);
        Task<List<Reminder>> GetByTaskIdAsync(int taskId);
        Task<Reminder> CreateAsync(Reminder reminder);
        Task CreateBatchAsync(IEnumerable<Reminder> reminders);
        Task MarkFiredAsync(int reminderId);
        Task DeleteByTaskIdAsync(int taskId);
    }
}
