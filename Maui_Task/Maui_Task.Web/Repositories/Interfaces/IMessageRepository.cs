using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Web.Repositories.Interfaces
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<IEnumerable<Message>> GetConversationAsync(int userId, int contactId);
        Task<IEnumerable<AppUser>> GetContactsAsync(int userId);
    }
}
