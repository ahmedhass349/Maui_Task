using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Notifications;

namespace Maui_Task.Shared.Services
{
    public interface INotificationDataService
    {
        Task<List<NotificationDto>> GetNotificationsAsync();
        Task<int> GetUnreadCountAsync();
        Task<bool> MarkAsReadAsync(int id);
        Task<bool> MarkAllAsReadAsync();
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteManyAsync(IEnumerable<int> ids);
        Task<bool> DeleteAllAsync();
    }
}