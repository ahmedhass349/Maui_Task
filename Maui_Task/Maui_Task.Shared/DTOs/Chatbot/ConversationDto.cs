using System;
using System.Collections.Generic;

namespace Maui_Task.Shared.DTOs.Chatbot
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public List<ChatbotMessageDto> Messages { get; set; } = new List<ChatbotMessageDto>();
    }
}
