using System;

namespace Maui_Task.Shared.DTOs.Auth
{
    /// <summary>
    /// Canonical User DTO - used across all endpoints for consistency
    /// </summary>
    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Company { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? Timezone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
