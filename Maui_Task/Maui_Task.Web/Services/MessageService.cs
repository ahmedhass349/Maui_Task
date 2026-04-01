// FILE: Services/MessageService.cs
// STATUS: UPDATED
// CHANGES: Fixed N+1 query in GetContactsAsync (#11), removed unused _mapper (#16),
//          added Initials/IsStarred to ContactDto (#29)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Messages;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;

        public MessageService(
            IMessageRepository messageRepository,
            IUserRepository userRepository)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<ContactDto>> GetContactsAsync(int userId)
        {
            var contacts = await _messageRepository.GetContactsAsync(userId);
            var contactList = contacts.ToList();

            // Fix #11: Batch-load all messages instead of N+1
            var contactIds = contactList.Select(c => c.Id).ToList();
            var allMessages = await _messageRepository.Query()
                .Where(m => (m.SenderId == userId && contactIds.Contains(m.ReceiverId)) ||
                            (m.ReceiverId == userId && contactIds.Contains(m.SenderId)))
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var result = new List<ContactDto>();
            foreach (var contact in contactList)
            {
                var contactMessages = allMessages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == contact.Id) ||
                                (m.ReceiverId == userId && m.SenderId == contact.Id))
                    .ToList();

                var lastMessage = contactMessages.FirstOrDefault();
                int unreadCount = contactMessages.Count(m => m.ReceiverId == userId && !m.IsRead);

                // Compute initials from name (#29)
                var nameParts = (contact.FullName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string initials = nameParts.Length >= 2
                    ? $"{nameParts[0][0]}{nameParts[^1][0]}".ToUpperInvariant()
                    : nameParts.Length == 1 ? nameParts[0][0].ToString().ToUpperInvariant() : "?";

                result.Add(new ContactDto
                {
                    Id = contact.Id,
                    Name = contact.FullName ?? string.Empty,
                    AvatarUrl = contact.AvatarUrl,
                    Initials = initials,
                    IsStarred = false, // MVP: no starred contacts feature yet
                    LastMessage = lastMessage?.Body ?? string.Empty,
                    LastMessageTime = lastMessage?.SentAt ?? DateTime.MinValue,
                    UnreadCount = unreadCount
                });
            }

            return result.OrderByDescending(c => c.LastMessageTime);
        }

        public async Task<IEnumerable<MessageDto>> GetConversationAsync(int userId, int contactId)
        {
            var messages = await _messageRepository.GetConversationAsync(userId, contactId);

            return messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender?.FullName ?? string.Empty,
                ReceiverId = m.ReceiverId,
                Body = m.Body,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            }).OrderBy(m => m.SentAt);
        }

        public async Task<MessageDto> SendMessageAsync(int userId, SendMessageRequest request)
        {
            var sender = await _userRepository.GetByIdAsync(userId);
            if (sender == null)
                throw new KeyNotFoundException("Sender not found.");

            var receiver = await _userRepository.GetByIdAsync(request.ReceiverId);
            if (receiver == null)
                throw new KeyNotFoundException($"Receiver with ID {request.ReceiverId} not found.");

            var message = new Message
            {
                SenderId = userId,
                ReceiverId = request.ReceiverId,
                Body = request.Body,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(message);
            await _messageRepository.SaveChangesAsync();

            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = sender.FullName,
                ReceiverId = message.ReceiverId,
                Body = message.Body,
                IsRead = message.IsRead,
                SentAt = message.SentAt
            };
        }
    }
}
