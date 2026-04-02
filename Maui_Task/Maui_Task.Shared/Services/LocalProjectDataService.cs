using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Projects;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Repositories.Interfaces;

namespace Maui_Task.Shared.Services
{
    public class LocalProjectDataService : IProjectDataService
    {
        private readonly IProjectRepository _projects;
        private readonly IUserRepository _users;
        private readonly IAuthService _auth;
        private readonly INotificationRepository _notifications;
        private readonly INotificationClientService _notificationClient;

        public LocalProjectDataService(
            IProjectRepository projects,
            IUserRepository users,
            IAuthService auth,
            INotificationRepository notifications,
            INotificationClientService notificationClient)
        {
            _projects = projects;
            _users = users;
            _auth = auth;
            _notifications = notifications;
            _notificationClient = notificationClient;
        }

        public async Task<List<ProjectDto>> GetProjectsAsync()
        {
            var userId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (userId <= 0)
            {
                return new List<ProjectDto>();
            }

            var projects = await _projects.GetAllByUserIdAsync(userId);
            return projects.Select(Map).ToList();
        }

        public async Task<ProjectDto?> GetProjectAsync(int id)
        {
            var project = await _projects.GetByIdAsync(id);
            return project == null ? null : Map(project);
        }

        public async Task<ProjectDto?> SaveProjectAsync(CreateProjectRequest request, int? projectId = null)
        {
            var currentUserId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (currentUserId <= 0)
            {
                return null;
            }

            Project entity;
            var isUpdate = projectId.HasValue && projectId.Value > 0;
            if (isUpdate)
            {
                entity = await _projects.GetByIdAsync(projectId!.Value) ?? new Project();
            }
            else
            {
                entity = new Project
                {
                    CreatedAt = DateTime.UtcNow
                };
            }

            entity.Name = request.Name;
            entity.Description = request.Description;
            entity.Color = string.IsNullOrWhiteSpace(request.Color) ? "#3B82F6" : request.Color;
            entity.OwnerId = entity.OwnerId == 0 ? currentUserId : entity.OwnerId;

            if (entity.Id == 0)
            {
                await _projects.CreateAsync(entity);
                await AddNotificationAsync(currentUserId, NotificationType.SystemAnnouncement, $"Project created: {entity.Name}", $"{entity.Name} is ready for local tracking.", null);
            }
            else
            {
                await _projects.UpdateAsync(entity);
                await AddNotificationAsync(currentUserId, NotificationType.SystemAnnouncement, $"Project updated: {entity.Name}", $"{entity.Name} was updated locally.", null);
            }

            var saved = await _projects.GetByIdAsync(entity.Id);
            return saved == null ? null : Map(saved);
        }

        public async Task DeleteProjectAsync(int id)
        {
            var project = await _projects.GetByIdAsync(id);
            if (project == null)
            {
                return;
            }

            await _projects.DeleteAsync(id);

            var currentUserId = await LocalUserResolver.ResolveCurrentUserIdAsync(_auth, _users);
            if (currentUserId > 0)
            {
                await AddNotificationAsync(currentUserId, NotificationType.SystemAnnouncement, $"Project deleted: {project.Name}", $"{project.Name} was removed.", null);
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
                Priority = NotificationPriority.Low,
                RelatedTaskId = relatedTaskId,
                CreatedAt = DateTime.UtcNow
            };

            await _notifications.CreateAsync(entity);
            _notificationClient.AddNotification(Map(entity));
        }

        private static ProjectDto Map(Project project)
        {
            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Color = project.Color,
                OwnerId = project.OwnerId,
                OwnerName = string.IsNullOrWhiteSpace(project.Owner?.FullName) ? "Unknown Owner" : project.Owner.FullName,
                IsStarred = project.IsStarred,
                CreatedAt = project.CreatedAt,
                TasksTotal = project.Tasks.Count,
                TasksCompleted = project.Tasks.Count(t => t.Status == Maui_Task.Shared.Data.Entities.TaskStatus.Completed),
                MemberCount = project.Members.Count
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