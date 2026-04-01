using System.ComponentModel.DataAnnotations;

namespace Maui_Task.Shared.DTOs.Messages
{
    public class SendMessageRequest
    {
        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;
    }
}
