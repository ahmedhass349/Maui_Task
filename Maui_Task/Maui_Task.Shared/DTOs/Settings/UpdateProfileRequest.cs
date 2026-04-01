// FILE: DTOs/Settings/UpdateProfileRequest.cs
// STATUS: UPDATED
// CHANGES: Replaced FullName with FirstName/LastName for frontend compatibility (#24).
//          Removed Data Annotations (using FluentValidation instead).

namespace Maui_Task.Shared.DTOs.Settings
{
    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Company { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? Timezone { get; set; }
    }
}
