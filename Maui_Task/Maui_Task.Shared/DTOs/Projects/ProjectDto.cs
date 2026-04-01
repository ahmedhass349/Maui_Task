using System;

namespace Maui_Task.Shared.DTOs.Projects
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public bool IsStarred { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TasksTotal { get; set; }
        public int TasksCompleted { get; set; }
        public int MemberCount { get; set; }
    }
}
