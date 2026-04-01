// FILE: Services/ChatbotService.cs
// STATUS: UPDATED
// CHANGES: Added userId ownership checks to Get/Send/Delete (#2),
//          Added echo bot response to SendMessageAsync (#19)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Chatbot;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IChatbotRepository _chatbotRepository;
        private readonly IGenericRepository<ChatbotMessage> _chatbotMessageRepository;
        private readonly IMapper _mapper;

        public ChatbotService(
            IChatbotRepository chatbotRepository,
            IGenericRepository<ChatbotMessage> chatbotMessageRepository,
            IMapper mapper)
        {
            _chatbotRepository = chatbotRepository;
            _chatbotMessageRepository = chatbotMessageRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConversationListDto>> GetConversationsAsync(int userId)
        {
            var conversations = await _chatbotRepository.GetUserConversationsAsync(userId);

            return conversations.Select(c => new ConversationListDto
            {
                Id = c.Id,
                Title = c.Title,
                UpdatedAt = c.UpdatedAt
            }).OrderByDescending(c => c.UpdatedAt);
        }

        public async Task<ConversationDto> GetConversationAsync(int userId, int conversationId)
        {
            var conversation = await _chatbotRepository.GetConversationWithMessagesAsync(conversationId);
            if (conversation == null)
                throw new KeyNotFoundException($"Conversation with ID {conversationId} not found.");

            // Ownership check (#2)
            if (conversation.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to access this conversation.");

            return new ConversationDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                UpdatedAt = conversation.UpdatedAt,
                Messages = conversation.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatbotMessageDto
                    {
                        Id = m.Id,
                        Role = m.Role,
                        Text = m.Text,
                        CreatedAt = m.CreatedAt
                    }).ToList()
            };
        }

        public async Task<ConversationDto> CreateConversationAsync(int userId, CreateConversationRequest request)
        {
            var conversation = new ChatbotConversation
            {
                Title = request.Title,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _chatbotRepository.AddAsync(conversation);
            await _chatbotRepository.SaveChangesAsync();

            return new ConversationDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                UpdatedAt = conversation.UpdatedAt,
                Messages = new List<ChatbotMessageDto>()
            };
        }

        public async Task<ChatbotMessageDto> SendMessageAsync(int userId, int conversationId, SendChatbotMessageRequest request)
        {
            var conversation = await _chatbotRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                throw new KeyNotFoundException($"Conversation with ID {conversationId} not found.");

            // Ownership check (#2)
            if (conversation.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to send messages in this conversation.");

            var userMessage = new ChatbotMessage
            {
                ConversationId = conversationId,
                Role = "user",
                Text = request.Text,
                CreatedAt = DateTime.UtcNow
            };

            await _chatbotMessageRepository.AddAsync(userMessage);

            // Echo bot response (#19)
            var botMessage = new ChatbotMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Text = $"I received your message: \"{request.Text}\". This is an MVP echo response.",
                CreatedAt = DateTime.UtcNow.AddMilliseconds(1)
            };

            await _chatbotMessageRepository.AddAsync(botMessage);

            conversation.UpdatedAt = DateTime.UtcNow;
            _chatbotRepository.Update(conversation);

            await _chatbotMessageRepository.SaveChangesAsync();

            // Return the bot's message so the caller sees the response
            return new ChatbotMessageDto
            {
                Id = botMessage.Id,
                Role = botMessage.Role,
                Text = botMessage.Text,
                CreatedAt = botMessage.CreatedAt
            };
        }

        public async Task DeleteConversationAsync(int userId, int conversationId)
        {
            var conversation = await _chatbotRepository.GetConversationWithMessagesAsync(conversationId);
            if (conversation == null)
                throw new KeyNotFoundException($"Conversation with ID {conversationId} not found.");

            // Ownership check (#2)
            if (conversation.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this conversation.");

            // Remove all messages first
            foreach (var message in conversation.Messages.ToList())
            {
                _chatbotMessageRepository.Remove(message);
            }

            _chatbotRepository.Remove(conversation);
            await _chatbotRepository.SaveChangesAsync();
        }
    }
}
