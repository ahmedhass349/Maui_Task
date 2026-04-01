using System;
using System.Collections.Generic;

namespace Maui_Task.Shared.Data.Entities
{
    public enum TaskPriority { Low, Medium, High }
    public enum TaskStatus { Todo, InProgress, Review, Completed }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ProjectId { get; set; }
        public int? AssigneeId { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskStatus Status { get; set; } = TaskStatus.Todo;
        public DateTime? DueDate { get; set; }
        public bool IsStarred { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Project? Project { get; set; }
        public AppUser? Assignee { get; set; }
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}
