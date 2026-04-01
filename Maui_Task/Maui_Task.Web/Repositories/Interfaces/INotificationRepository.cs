using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Web.Repositories.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
        Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, int page, int pageSize);
        Task<int> GetUnreadCountAsync(int userId);
        Task<IEnumerable<Notification>> GetUnreadAsync(int userId);
        Task<Notification> CreateAsync(Notification notification);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAsync(int notificationId, int userId);
    }
}
