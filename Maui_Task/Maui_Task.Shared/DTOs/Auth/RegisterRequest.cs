using System.ComponentModel.DataAnnotations;

namespace Maui_Task.Shared.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? Company { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? Timezone { get; set; }
    }
}
