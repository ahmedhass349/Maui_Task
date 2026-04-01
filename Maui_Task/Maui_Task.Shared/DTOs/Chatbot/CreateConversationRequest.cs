using System.ComponentModel.DataAnnotations;

namespace Maui_Task.Shared.DTOs.Chatbot
{
    public class CreateConversationRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
    }
}
