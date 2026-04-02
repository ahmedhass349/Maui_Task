using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Messages;

namespace Maui_Task.Shared.Services
{
    public interface IMessageDataService
    {
        Task<int> GetCurrentUserIdAsync();
        Task<UserDto?> GetCurrentUserAsync();
        Task<List<ContactDto>> GetContactsAsync();
        Task<List<MessageDto>> GetConversationAsync(int contactId);
        Task<MessageDto?> SendMessageAsync(SendMessageRequest request);
    }
}