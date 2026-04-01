// FILE: DTOs/Tasks/TaskDto.cs
// STATUS: UPDATED
// CHANGES: Added DueDateLabel computed field (#28), kept ProjectName/AssigneeName (frontend maps these)

using System;

namespace Maui_Task.Shared.DTOs.Tasks
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? AssigneeName { get; set; }
        public DateTime? DueDate { get; set; }
        public string? DueDateLabel { get; set; }   // Formatted string e.g. "Oct 15" for frontend (#28)
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsStarred { get; set; }
        public DateTime CreatedAt { get; set; }

        public TaskDto Clone()
        {
            return new TaskDto
            {
                Id = Id,
                Title = Title,
                Description = Description,
                ProjectName = ProjectName,
                AssigneeName = AssigneeName,
                DueDate = DueDate,
                DueDateLabel = DueDateLabel,
                Priority = Priority,
                Status = Status,
                IsStarred = IsStarred,
                CreatedAt = CreatedAt
            };
        }
    }
}
