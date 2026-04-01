using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/ChatbotController.cs
// STATUS: MODIFIED
// CHANGES: Fixed GetUserId() (#3), removed try-catch (#15), pass userId to Get/Send/Delete (#2), cleaned usings (#17)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maui_Task.Shared.DTOs.Chatbot;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// API controller for managing chatbot conversations and messages.
    /// </summary>
    [ApiController]
    [Route("api/chatbot")]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Retrieves all conversations for the authenticated user.
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetUserId();
            var conversations = await _chatbotService.GetConversationsAsync(userId);
            return Ok(ApiResponse<IEnumerable<ConversationListDto>>.Ok(conversations));
        }

        /// <summary>
        /// Retrieves a specific conversation by its identifier, including all messages.
        /// </summary>
        [HttpGet("conversations/{id}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            var userId = GetUserId();
            var conversation = await _chatbotService.GetConversationAsync(userId, id);
            return Ok(ApiResponse<ConversationDto>.Ok(conversation));
        }

        /// <summary>
        /// Creates a new conversation for the authenticated user.
        /// </summary>
        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            var userId = GetUserId();
            var conversation = await _chatbotService.CreateConversationAsync(userId, request);
            return StatusCode(201, ApiResponse<ConversationDto>.Ok(conversation, "Conversation created successfully."));
        }

        /// <summary>
        /// Sends a message to an existing conversation and receives a chatbot response.
        /// </summary>
        [HttpPost("conversations/{id}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendChatbotMessageRequest request)
        {
            var userId = GetUserId();
            var message = await _chatbotService.SendMessageAsync(userId, id, request);
            return StatusCode(201, ApiResponse<ChatbotMessageDto>.Ok(message, "Message sent successfully."));
        }

        /// <summary>
        /// Deletes a conversation and all its messages.
        /// </summary>
        [HttpDelete("conversations/{id}")]
        public async Task<IActionResult> DeleteConversation(int id)
        {
            var userId = GetUserId();
            await _chatbotService.DeleteConversationAsync(userId, id);
            return NoContent();
        }
    }
}
