using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Tasks;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Repositories.Interfaces;

namespace Maui_Task.Shared.Services
{
    public class LocalTaskDataService : ITaskDataService
    {
        private readonly ITaskRepository _tasks;
        private readonly IUserRepository _users;
        private readonly IAuthService _auth;
        private readonly INotificationRepository _notifications;
        private readonly INotificationClientService _notificationClient;

        public LocalTaskDataService(
            ITaskRepository tasks,
            IUserRepository users,
            IAuthService auth,
            INotificationRepository notifications,
            INotificationClientService notificationClient)
        {
            _tasks = tasks;
            _users = users;
            _auth = auth;
            _notifications = notifications;
            _notificationClient = notificationClient;
        }

        public async Task<List<TaskDto>> GetTasksAsync()
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return new List<TaskDto>();
            }

            var tasks = await _tasks.GetAllByUserIdAsync(userId);
            return tasks.Select(Map).ToList();
        }

        public async Task<TaskDto?> GetTaskAsync(int id)
        {
            var task = await _tasks.GetByIdAsync(id);
            return task == null ? null : Map(task);
        }

        public async Task<TaskDto?> SaveTaskAsync(CreateTaskRequest request, int? taskId = null)
        {
            var currentUserId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (currentUserId <= 0)
            {
                return null;
            }

            TaskItem entity;
            var isUpdate = taskId.HasValue && taskId.Value > 0;
            if (isUpdate)
            {
                entity = await _tasks.GetByIdAsync(taskId!.Value) ?? new TaskItem();
                if (entity.Id == 0)
                {
                    entity.AssigneeId = request.AssigneeId ?? currentUserId;
                    entity.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                entity = new TaskItem
                {
                    CreatedAt = DateTime.UtcNow
                };
            }

            entity.Title = request.Title;
            entity.Description = request.Description;
            entity.ProjectId = request.ProjectId;
            entity.AssigneeId = request.AssigneeId ?? entity.AssigneeId ?? currentUserId;
            entity.Priority = request.Priority;
            entity.Status = request.Status;
            entity.DueDate = request.DueDate;

            if (entity.Id == 0)
            {
                await _tasks.CreateAsync(entity);
                await AddNotificationAsync(currentUserId, NotificationType.TaskCreated, $"Task created: {entity.Title}", $"{entity.Title} was added to your task list.", entity.Id);
            }
            else
            {
                await _tasks.UpdateAsync(entity);
                await AddNotificationAsync(currentUserId, NotificationType.TaskUpdated, $"Task updated: {entity.Title}", $"{entity.Title} was updated locally.", entity.Id);
            }

            var saved = await _tasks.GetByIdAsync(entity.Id);
            return saved == null ? null : Map(saved);
        }

        public async Task<TaskDto?> ToggleStarAsync(int taskId)
        {
            var task = await _tasks.GetByIdAsync(taskId);
            if (task == null)
            {
                return null;
            }

            task.IsStarred = !task.IsStarred;
            await _tasks.UpdateAsync(task);
            return Map(task);
        }

        public async Task<TaskDto?> UpdateStatusAsync(int taskId, string status)
        {
            var task = await _tasks.GetByIdAsync(taskId);
            if (task == null)
            {
                return null;
            }

            if (!Enum.TryParse<Maui_Task.Shared.Data.Entities.TaskStatus>(status, true, out var parsedStatus))
            {
                return Map(task);
            }

            task.Status = parsedStatus;
            await _tasks.UpdateAsync(task);

            var currentUserId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (currentUserId > 0)
            {
                await AddNotificationAsync(currentUserId, NotificationType.TaskUpdated, $"Task status updated: {task.Title}", $"{task.Title} is now {task.Status}.", task.Id);
            }

            return Map(task);
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            var task = await _tasks.GetByIdAsync(taskId);
            if (task == null)
            {
                return;
            }

            await _tasks.DeleteAsync(taskId);

            var currentUserId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (currentUserId > 0)
            {
                await AddNotificationAsync(currentUserId, NotificationType.TaskDeleted, $"Task deleted: {task.Title}", $"{task.Title} was removed.", task.Id);
            }
        }

        private async Task AddNotificationAsync(int userId, NotificationType type, string title, string message, int? relatedTaskId)
        {
            var entity = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Priority = NotificationPriority.Medium,
                RelatedTaskId = relatedTaskId,
                CreatedAt = DateTime.UtcNow
            };

            await _notifications.CreateAsync(entity);
            _notificationClient.AddNotification(Map(entity));
        }

        private static TaskDto Map(TaskItem task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                ProjectName = task.Project?.Name ?? "No Project",
                AssigneeName = task.Assignee?.FullName,
                DueDate = task.DueDate,
                DueDateLabel = task.DueDate?.ToString("MMM dd"),
                Priority = task.Priority.ToString(),
                Status = task.Status.ToString(),
                IsStarred = task.IsStarred,
                CreatedAt = task.CreatedAt
            };
        }

        private static NotificationDto Map(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                Priority = notification.Priority.ToString(),
                IsRead = notification.IsRead,
                ActionUrl = notification.ActionUrl,
                RelatedTaskId = notification.RelatedTaskId,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt,
                TimeAgo = string.Empty
            };
        }
    }
}