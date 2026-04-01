using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/MessagesController.cs
// STATUS: MODIFIED
// CHANGES: Fixed GetUserId() (#3), removed try-catch (#15), cleaned usings (#17), standardized route (#20)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maui_Task.Shared.DTOs.Messages;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// API controller for managing messages and conversations.
    /// </summary>
    [ApiController]
    [Route("api/messages")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Retrieves all contacts (users with conversation history) for the authenticated user.
        /// </summary>
        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var userId = GetUserId();
            var contacts = await _messageService.GetContactsAsync(userId);
            return Ok(ApiResponse<IEnumerable<ContactDto>>.Ok(contacts, "Contacts retrieved successfully"));
        }

        /// <summary>
        /// Retrieves the full conversation between the authenticated user and a specific contact.
        /// </summary>
        [HttpGet("{contactId}")]
        public async Task<IActionResult> GetConversation(int contactId)
        {
            var userId = GetUserId();
            var messages = await _messageService.GetConversationAsync(userId, contactId);
            return Ok(ApiResponse<IEnumerable<MessageDto>>.Ok(messages, "Conversation retrieved successfully"));
        }

        /// <summary>
        /// Sends a new message from the authenticated user to a specified receiver.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = GetUserId();
            var message = await _messageService.SendMessageAsync(userId, request);
            return StatusCode(201, ApiResponse<MessageDto>.Ok(message, "Message sent successfully"));
        }
    }
}
