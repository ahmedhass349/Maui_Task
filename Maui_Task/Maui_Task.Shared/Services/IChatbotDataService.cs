using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Chatbot;

namespace Maui_Task.Shared.Services
{
    public interface IChatbotDataService
    {
        Task<List<ConversationListDto>> GetConversationsAsync();
        Task<ConversationDto?> GetConversationAsync(int id);
        Task<ConversationDto?> CreateConversationAsync(CreateConversationRequest request);
        Task<ChatbotMessageDto?> SendMessageAsync(int conversationId, SendChatbotMessageRequest request);
        Task<bool> DeleteConversationAsync(int conversationId);
    }
}