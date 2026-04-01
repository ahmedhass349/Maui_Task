using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Messages;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface IMessageService
    {
        Task<IEnumerable<ContactDto>> GetContactsAsync(int userId);
        Task<IEnumerable<MessageDto>> GetConversationAsync(int userId, int contactId);
        Task<MessageDto> SendMessageAsync(int userId, SendMessageRequest request);
    }
}
