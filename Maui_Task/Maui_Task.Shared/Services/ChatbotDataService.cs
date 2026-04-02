using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Chatbot;
using Maui_Task.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Services
{
    public class ChatbotDataService : IChatbotDataService
    {
        private readonly HttpApiService _api;
        private readonly AppDbContext _db;
        private readonly TaskFlowAuthStateProvider _authState;
        private readonly ISyncQueueService _syncQueue;

        public ChatbotDataService(HttpApiService api, AppDbContext db, TaskFlowAuthStateProvider authState, ISyncQueueService syncQueue)
        {
            _api = api;
            _db = db;
            _authState = authState;
            _syncQueue = syncQueue;
        }

        public async Task<List<ConversationListDto>> GetConversationsAsync()
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<IEnumerable<ConversationListDto>>>("/api/chatbot/conversations");
                if (response?.Data != null)
                {
                    return response.Data.OrderByDescending(c => c.UpdatedAt).ToList();
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var userId = await ResolveCurrentUserIdAsync();
            return await _db.ChatbotConversations
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new ConversationListDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<ConversationDto?> GetConversationAsync(int id)
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<ConversationDto>>($"/api/chatbot/conversations/{id}");
                if (response?.Data != null)
                {
                    return response.Data;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var local = await _db.ChatbotConversations
                .AsNoTracking()
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id);

            return local is null ? null : Map(local);
        }

        public async Task<ConversationDto?> CreateConversationAsync(CreateConversationRequest request)
        {
            try
            {
                var response = await _api.PostAsync<ApiResponse<ConversationDto>>("/api/chatbot/conversations", request);
                if (response?.Data != null)
                {
                    return response.Data;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var userId = await ResolveCurrentUserIdAsync();
            var conversation = new ChatbotConversation
            {
                Title = request.Title.Trim(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.ChatbotConversations.Add(conversation);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Chatbot", "create", new ChatbotConversationSyncPayload(request));
            return Map(conversation);
        }

        public async Task<ChatbotMessageDto?> SendMessageAsync(int conversationId, SendChatbotMessageRequest request)
        {
            try
            {
                var response = await _api.PostAsync<ApiResponse<ChatbotMessageDto>>($"/api/chatbot/conversations/{conversationId}/messages", request);
                if (response?.Data != null)
                {
                    return response.Data;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var conversation = await _db.ChatbotConversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation is null)
            {
                return null;
            }

            var userMessage = new ChatbotMessage
            {
                ConversationId = conversationId,
                Role = "user",
                Text = request.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var assistantMessage = new ChatbotMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Text = "Offline mode: message saved locally.",
                CreatedAt = DateTime.UtcNow.AddMilliseconds(1)
            };

            conversation.UpdatedAt = DateTime.UtcNow;
            _db.ChatbotMessages.AddRange(userMessage, assistantMessage);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Chatbot", "send", new ChatbotMessageSyncPayload(conversationId, request));

            return new ChatbotMessageDto
            {
                Id = userMessage.Id,
                Role = userMessage.Role,
                Text = userMessage.Text,
                CreatedAt = userMessage.CreatedAt
            };
        }

        public async Task<bool> DeleteConversationAsync(int conversationId)
        {
            try
            {
                await _api.DeleteAsync($"/api/chatbot/conversations/{conversationId}");
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var local = await _db.ChatbotConversations.FirstOrDefaultAsync(c => c.Id == conversationId);
            if (local is null)
            {
                return false;
            }

            _db.ChatbotConversations.Remove(local);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Chatbot", "delete", new ChatbotConversationIdSyncPayload(conversationId));
            return true;
        }

        private async Task<int> ResolveCurrentUserIdAsync()
        {
            var currentUserId = _authState.CurrentUser?.Id ?? 0;
            if (currentUserId > 0)
            {
                return currentUserId;
            }

            var localUser = await _db.AppUsers.OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt).FirstOrDefaultAsync();
            return localUser?.Id ?? 0;
        }

        private static ConversationDto Map(ChatbotConversation conversation)
        {
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
                    })
                    .ToList()
            };
        }
    }
}