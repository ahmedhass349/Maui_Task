using System;
using System.Collections.Generic;

namespace Maui_Task.Shared.Data.Entities
{
    public class ChatbotConversation
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser User { get; set; } = null!;
        public ICollection<ChatbotMessage> Messages { get; set; } = new List<ChatbotMessage>();
    }
}
