using System;

namespace Maui_Task.Shared.Data.Entities
{
    public class TaskComment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int AuthorId { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public TaskItem Task { get; set; } = null!;
        public AppUser Author { get; set; } = null!;
    }
}
