// FILE: DTOs/TaskComments/UpdateTaskCommentRequest.cs
// STATUS: NEW
// CHANGES: Created for fully exposing TaskComment entity (#21)

namespace Maui_Task.Shared.DTOs.TaskComments
{
    public class UpdateTaskCommentRequest
    {
        public string Body { get; set; } = string.Empty;
    }
}
