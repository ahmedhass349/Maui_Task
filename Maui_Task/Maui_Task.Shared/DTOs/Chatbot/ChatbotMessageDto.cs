using System;

namespace Maui_Task.Shared.DTOs.Chatbot
{
    public class ChatbotMessageDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;   // "user" or "assistant"
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
