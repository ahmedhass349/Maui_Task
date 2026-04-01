using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Web.Repositories.Interfaces
{
    public interface IChatbotRepository : IGenericRepository<ChatbotConversation>
    {
        Task<IEnumerable<ChatbotConversation>> GetUserConversationsAsync(int userId);
        Task<ChatbotConversation?> GetConversationWithMessagesAsync(int conversationId);
    }
}
