// FILE: DTOs/TaskComments/TaskCommentDto.cs
// STATUS: NEW
// CHANGES: Created for fully exposing TaskComment entity (#21)

using System;

namespace Maui_Task.Shared.DTOs.TaskComments
{
    public class TaskCommentDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
