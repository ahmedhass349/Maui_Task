using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Web.Repositories.Interfaces;

namespace Maui_Task.Web.Repositories
{
    public class ChatbotRepository : GenericRepository<ChatbotConversation>, IChatbotRepository
    {
        public ChatbotRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ChatbotConversation>> GetUserConversationsAsync(int userId)
        {
            return await _dbSet
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<ChatbotConversation?> GetConversationWithMessagesAsync(int conversationId)
        {
            return await _dbSet
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }
    }
}
