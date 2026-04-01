// FILE: Services/TaskService.cs
// STATUS: UPDATED
// CHANGES: Added userId ownership checks to Update/Delete/ToggleStar/UpdateStatus (#2),
//          Fixed in-memory search to DB query (#8),
//          Fixed double-fetch pattern (#13)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Tasks;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;
using TaskStatus = Maui_Task.Shared.Data.Entities.TaskStatus;

namespace Maui_Task.Web.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IReminderService _reminderService;

        public TaskService(ITaskRepository taskRepository, IMapper mapper, ILogger<TaskService> logger, INotificationService notificationService, IReminderService reminderService)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
            _reminderService = reminderService;
        }

        public async Task<IEnumerable<TaskDto>> GetTasksAsync(int userId, TaskFilterRequest filter)
        {
            string? statusStr = filter.Status?.ToString();
            string? priorityStr = filter.Priority?.ToString();

            // Fix #8: Add search to the DB query instead of in-memory
            var query = _taskRepository.Query()
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Where(t => t.AssigneeId == userId);

            if (!string.IsNullOrEmpty(statusStr))
            {
                if (Enum.TryParse<TaskStatus>(statusStr, ignoreCase: true, out var parsedStatus))
                {
                    query = query.Where(t => t.Status == parsedStatus);
                }
            }

            if (!string.IsNullOrEmpty(priorityStr))
            {
                if (Enum.TryParse<TaskPriority>(priorityStr, ignoreCase: true, out var parsedPriority))
                {
                    query = query.Where(t => t.Priority == parsedPriority);
                }
            }

            if (filter.ProjectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == filter.ProjectId.Value);
            }

            if (filter.IsStarred.HasValue)
            {
                query = query.Where(t => t.IsStarred == filter.IsStarred.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(search) ||
                                         (t.Description != null && t.Description.ToLower().Contains(search)));
            }

            var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            return _mapper.Map<IEnumerable<TaskDto>>(tasks);
        }

        public async Task<TaskDto> GetTaskByIdAsync(int taskId)
        {
            var task = await _taskRepository.Query()
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");

            return _mapper.Map<TaskDto>(task);
        }

        public async Task<TaskDto> CreateTaskAsync(int userId, CreateTaskRequest request)
        {
            var task = new TaskItem
            {
                Title = request.Title,
                Description = request.Description,
                ProjectId = request.ProjectId,
                AssigneeId = request.AssigneeId ?? userId,
                Priority = request.Priority,
                Status = request.Status,
                DueDate = request.DueDate,
                CreatedAt = DateTime.UtcNow
            };

            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();

            // Schedule reminders if provided
            if (request.ReminderMap != null && (request.NotifyEmail || request.NotifyInApp))
            {
                try
                {
                    var reminderDto = new CreateReminderDto
                    {
                        TaskId = task.Id,
                        ReminderMap = request.ReminderMap,
                        NotifyEmail = request.NotifyEmail,
                        NotifyInApp = request.NotifyInApp
                    };
                    await _reminderService.SaveRemindersAsync(reminderDto, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to schedule reminders for task {Id}", task.Id);
                    // Don't fail task creation if reminder scheduling fails
                }
            }

            // Send notification
            try
            {
                await _notificationService.NotifyTaskCreatedAsync(userId, task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification for task {Id}", task.Id);
                // Don't fail task creation if notification fails
            }

            return await GetTaskByIdAsync(task.Id);
        }

        public async Task<TaskDto> UpdateTaskAsync(int userId, int taskId, UpdateTaskRequest request)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");

            // Ownership check (#2)
            if (task.AssigneeId != userId)
                throw new UnauthorizedAccessException("You do not have permission to update this task.");

            task.Title = request.Title;
            task.Description = request.Description;
            task.AssigneeId = request.AssigneeId ?? task.AssigneeId;
            task.Priority = request.Priority;
            task.Status = request.Status;
            task.DueDate = request.DueDate;

            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync();

            return await GetTaskByIdAsync(task.Id);
        }

        public async Task DeleteTaskAsync(int userId, int taskId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");

            // Ownership check (#2)
            if (task.AssigneeId != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this task.");

            _taskRepository.Remove(task);
            await _taskRepository.SaveChangesAsync();
        }

        public async Task<TaskDto> ToggleStarAsync(int userId, int taskId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");

            // Ownership check (#2)
            if (task.AssigneeId != userId)
                throw new UnauthorizedAccessException("You do not have permission to modify this task.");

            task.IsStarred = !task.IsStarred;

            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync();

            return await GetTaskByIdAsync(task.Id);
        }

        public async Task<TaskDto> UpdateStatusAsync(int userId, int taskId, string status)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");

            // Ownership check (#2)
            if (task.AssigneeId != userId)
                throw new UnauthorizedAccessException("You do not have permission to modify this task.");

            if (!Enum.TryParse<TaskStatus>(status, ignoreCase: true, out var parsedStatus))
                throw new ArgumentException($"Invalid status value: {status}");

            task.Status = parsedStatus;

            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync();

            return await GetTaskByIdAsync(task.Id);
        }
    }
}
