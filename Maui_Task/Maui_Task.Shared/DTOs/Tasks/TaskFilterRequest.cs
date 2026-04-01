using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.DTOs.Tasks
{
    public class TaskFilterRequest
    {
        public Maui_Task.Shared.Data.Entities.TaskStatus? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public int? ProjectId { get; set; }
        public bool? IsStarred { get; set; }
        public string? Search { get; set; }
    }
}
