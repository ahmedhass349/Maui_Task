// FILE: DTOs/Messages/ContactDto.cs
// STATUS: UPDATED
// CHANGES: Added Initials, IsStarred fields for frontend compatibility (#29)

using System;

namespace Maui_Task.Shared.DTOs.Messages
{
    public class ContactDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;       // Computed: e.g. "JD" for "John Doe" (#29)
        public string? AvatarUrl { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public bool IsStarred { get; set; }                        // For frontend compatibility (#29)
    }
}
