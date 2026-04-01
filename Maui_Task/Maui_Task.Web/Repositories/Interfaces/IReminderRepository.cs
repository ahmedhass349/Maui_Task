using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Web.Repositories.Interfaces
{
    public interface IReminderRepository : IGenericRepository<Reminder>
    {
        Task<IEnumerable<Reminder>> GetPendingAsync(DateTime upTo);
        Task<IEnumerable<Reminder>> GetByTaskIdAsync(int taskId);
        Task<Reminder> CreateAsync(Reminder reminder);
        Task CreateBatchAsync(IEnumerable<Reminder> reminders);
        Task MarkFiredAsync(int reminderId);
        Task DeleteByTaskIdAsync(int taskId);
    }
}
