using System;

namespace Maui_Task.Shared.DTOs.Chatbot
{
    public class ConversationListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
