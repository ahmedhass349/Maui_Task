using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Messages;
using Maui_Task.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Services
{
    public class MessageDataService : IMessageDataService
    {
        private readonly HttpApiService _api;
        private readonly AppDbContext _db;
        private readonly TaskFlowAuthStateProvider _authState;
        private readonly ISyncQueueService _syncQueue;

        public MessageDataService(HttpApiService api, AppDbContext db, TaskFlowAuthStateProvider authState, ISyncQueueService syncQueue)
        {
            _api = api;
            _db = db;
            _authState = authState;
            _syncQueue = syncQueue;
        }

        public async Task<int> GetCurrentUserIdAsync()
        {
            var currentUserId = _authState.CurrentUser?.Id ?? 0;
            if (currentUserId > 0)
            {
                return currentUserId;
            }

            var localUser = await _db.AppUsers.OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt).FirstOrDefaultAsync();
            return localUser?.Id ?? 0;
        }

        public async Task<UserDto?> GetCurrentUserAsync()
        {
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId <= 0)
            {
                return null;
            }

            var user = await _db.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (user is null)
            {
                return null;
            }

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Company = user.Company,
                Country = user.Country,
                Phone = user.Phone,
                Timezone = user.Timezone,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<List<ContactDto>> GetContactsAsync()
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<IEnumerable<ContactDto>>>("/api/messages/contacts");
                if (response?.Data != null)
                {
                    return response.Data.ToList();
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            return await LoadLocalContactsAsync();
        }

        public async Task<List<MessageDto>> GetConversationAsync(int contactId)
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<IEnumerable<MessageDto>>>($"/api/messages/{contactId}");
                if (response?.Data != null)
                {
                    return response.Data.OrderBy(m => m.SentAt).ToList();
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var currentUserId = await GetCurrentUserIdAsync();
            var localMessages = await _db.Messages
                .AsNoTracking()
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == contactId) || (m.SenderId == contactId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return localMessages.Select(Map).ToList();
        }

        public async Task<MessageDto?> SendMessageAsync(SendMessageRequest request)
        {
            try
            {
                var response = await _api.PostAsync<ApiResponse<MessageDto>>("/api/messages", request);
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

            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId <= 0)
            {
                return null;
            }

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = request.ReceiverId,
                Body = request.Body.Trim(),
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Message", "send", new MessageSyncPayload(request));

            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = await ResolveDisplayNameAsync(currentUserId),
                ReceiverId = message.ReceiverId,
                Body = message.Body,
                IsRead = message.IsRead,
                SentAt = message.SentAt
            };
        }

        private async Task<List<ContactDto>> LoadLocalContactsAsync()
        {
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId <= 0)
            {
                return new List<ContactDto>();
            }

            var messages = await _db.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return messages
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Select(group =>
                {
                    var latest = group.First();
                    var contact = latest.SenderId == currentUserId ? latest.Receiver : latest.Sender;
                    return new ContactDto
                    {
                        Id = contact.Id,
                        Name = contact.FullName,
                        Initials = BuildInitials(contact.FullName),
                        AvatarUrl = contact.AvatarUrl,
                        LastMessage = latest.Body,
                        LastMessageTime = latest.SentAt,
                        UnreadCount = group.Count(m => m.ReceiverId == currentUserId && !m.IsRead),
                        IsStarred = false
                    };
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();
        }

        private async Task<string> ResolveDisplayNameAsync(int userId)
        {
            var user = await _db.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return user?.FullName ?? "You";
        }

        private static MessageDto Map(Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = message.Sender?.FullName ?? string.Empty,
                ReceiverId = message.ReceiverId,
                Body = message.Body,
                IsRead = message.IsRead,
                SentAt = message.SentAt
            };
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0]));
            return new string(parts.ToArray());
        }
    }
}