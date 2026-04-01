using System;

namespace Maui_Task.Shared.Data.Entities
{
    public class ChatbotMessage
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string Role { get; set; } = string.Empty;   // "user" or "assistant"
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ChatbotConversation Conversation { get; set; } = null!;
    }
}
