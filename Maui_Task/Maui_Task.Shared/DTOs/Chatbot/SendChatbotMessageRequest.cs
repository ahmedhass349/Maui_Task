using System.ComponentModel.DataAnnotations;

namespace Maui_Task.Shared.DTOs.Chatbot
{
    public class SendChatbotMessageRequest
    {
        [Required]
        public string Text { get; set; } = string.Empty;
    }
}
