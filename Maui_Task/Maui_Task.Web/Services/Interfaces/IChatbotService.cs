using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Chatbot;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface IChatbotService
    {
        Task<IEnumerable<ConversationListDto>> GetConversationsAsync(int userId);
        Task<ConversationDto> GetConversationAsync(int userId, int conversationId);
        Task<ConversationDto> CreateConversationAsync(int userId, CreateConversationRequest request);
        Task<ChatbotMessageDto> SendMessageAsync(int userId, int conversationId, SendChatbotMessageRequest request);
        Task DeleteConversationAsync(int userId, int conversationId);
    }
}
