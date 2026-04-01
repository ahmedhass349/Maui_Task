// FILE: DTOs/Settings/ProfileDto.cs
// STATUS: UPDATED
// CHANGES: Now uses canonical UserDto structure for consistency

using Maui_Task.Shared.DTOs.Auth;

namespace Maui_Task.Shared.DTOs.Settings
{
    /// <summary>
    /// Profile DTO - uses canonical UserDto for consistency
    /// </summary>
    public class ProfileDto : UserDto
    {
        // Inherits all properties from UserDto
        // No additional properties needed - use canonical shape
    }
}
