using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Notifications;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface IReminderService
    {
        Task SaveRemindersAsync(CreateReminderDto dto, int userId);
        Task ProcessPendingRemindersAsync();
        Task DeleteRemindersForTaskAsync(int taskId);
    }
}
