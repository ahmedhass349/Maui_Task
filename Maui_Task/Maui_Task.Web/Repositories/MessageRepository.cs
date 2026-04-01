using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Web.Repositories.Interfaces;

namespace Maui_Task.Web.Repositories
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Message>> GetConversationAsync(int userId, int contactId)
        {
            return await _dbSet
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == userId && m.ReceiverId == contactId)
                         || (m.SenderId == contactId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AppUser>> GetContactsAsync(int userId)
        {
            var sentToIds = _dbSet
                .Where(m => m.SenderId == userId)
                .Select(m => m.ReceiverId);

            var receivedFromIds = _dbSet
                .Where(m => m.ReceiverId == userId)
                .Select(m => m.SenderId);

            var contactIds = sentToIds.Union(receivedFromIds);

            return await _context.AppUsers
                .Where(u => contactIds.Contains(u.Id))
                .ToListAsync();
        }
    }
}
