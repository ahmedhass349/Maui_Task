// FILE: DTOs/Tasks/UpdateStatusRequest.cs
// STATUS: NEW
// CHANGES: Extracted from inline UpdateStatusBody class in TasksController (#5)

namespace Maui_Task.Shared.DTOs.Tasks
{
    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
