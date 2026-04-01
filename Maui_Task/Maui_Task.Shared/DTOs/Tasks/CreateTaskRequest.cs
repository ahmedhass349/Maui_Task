using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.DTOs.Tasks
{
    public class CreateTaskRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? ProjectId { get; set; }

        public int? AssigneeId { get; set; }

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Required]
        public Maui_Task.Shared.Data.Entities.TaskStatus Status { get; set; } = Maui_Task.Shared.Data.Entities.TaskStatus.Todo;

        public DateTime? DueDate { get; set; }

        public Dictionary<string, List<string>>? ReminderMap { get; set; }

        public bool NotifyEmail { get; set; } = true;

        public bool NotifyInApp { get; set; } = true;
    }
}
