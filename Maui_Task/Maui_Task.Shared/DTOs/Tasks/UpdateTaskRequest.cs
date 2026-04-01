using System;
using System.ComponentModel.DataAnnotations;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.DTOs.Tasks
{
    public class UpdateTaskRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public int? AssigneeId { get; set; }

        [Required]
        public TaskPriority Priority { get; set; }

        [Required]
        public Maui_Task.Shared.Data.Entities.TaskStatus Status { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
