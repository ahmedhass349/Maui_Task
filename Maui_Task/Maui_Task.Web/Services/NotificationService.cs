/*
    CLEANUP [Maui_Task.Web/Services/NotificationService.cs]:
    - Removed: none
    - Fixed: Replaced hardcoded base URL construction with IConfiguration-driven AppSettings:BaseUrl lookup.
    - Moved: none
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;
using Maui_Task.Web.Hubs;

namespace Maui_Task.Web.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IMapper mapper,
            IHubContext<NotificationHub> hubContext,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _hubContext = hubContext;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        // Core creation method
        public async Task<NotificationDto> CreateAsync(int userId, string title, string message,
            NotificationType type, NotificationPriority priority,
            string? actionUrl = null, int? relatedTaskId = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Priority = priority,
                    ActionUrl = actionUrl,
                    RelatedTaskId = relatedTaskId
                };

                var createdNotification = await _notificationRepository.CreateAsync(notification);
                var notificationDto = _mapper.Map<NotificationDto>(createdNotification);

                // Compute TimeAgo
                notificationDto.TimeAgo = GetTimeAgo(createdNotification.CreatedAt);

                // Push to user via SignalR
                await _hubContext.Clients
                    .User(userId.ToString())
                    .SendAsync("ReceiveNotification", notificationDto);

                // Update unread count
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);
                await _hubContext.Clients
                    .User(userId.ToString())
                    .SendAsync("UnreadCount", unreadCount);

                _logger.LogInformation("Notification created: {Title} for User {UserId}", title, userId);

                return notificationDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
                throw;
            }
        }

        // Convenience methods per event
        public async Task NotifyTaskCreatedAsync(int userId, TaskItem task)
        {
            var title = "New Task Assigned";
            var message = $"You've been assigned a new task: {task.Title}";
            var priority = task.Priority switch
            {
                TaskPriority.High => NotificationPriority.High,
                TaskPriority.Low => NotificationPriority.Low,
                _ => NotificationPriority.Medium
            };

            await CreateAsync(userId, title, message, NotificationType.TaskCreated, priority,
                $"/tasks/{task.Id}", task.Id);
        }

        public async Task NotifyTaskUpdatedAsync(int userId, TaskItem task, string whatChanged)
        {
            var title = "Task Updated";
            var message = $"Task '{task.Title}' was updated: {whatChanged}";

            await CreateAsync(userId, title, message, NotificationType.TaskUpdated, NotificationPriority.Low,
                $"/tasks/{task.Id}", task.Id);
        }

        public async Task NotifyTaskDeletedAsync(int userId, string taskTitle)
        {
            var title = "Task Deleted";
            var message = $"Task '{taskTitle}' has been deleted";

            await CreateAsync(userId, title, message, NotificationType.TaskDeleted, NotificationPriority.Medium);
        }

        public async Task NotifyTaskDueSoonAsync(int userId, TaskItem task, string timeframe)
        {
            var title = $"Task Due {timeframe}";
            var message = $"Your task '{task.Title}' is due {timeframe}";
            var priority = timeframe.Contains("hour") ? NotificationPriority.Critical : NotificationPriority.High;

            await CreateAsync(userId, title, message, NotificationType.TaskDueSoon, priority,
                $"/tasks/{task.Id}", task.Id);

            // Send email notification
            try
            {
                var user = await GetUserById(userId);
                if (user != null)
                {
                    var taskUrl = $"{GetBaseUrl()}/tasks/{task.Id}";
                    await _emailService.SendTaskDueSoonEmailAsync(user.Email, user.FirstName, task.Title, timeframe, taskUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send due soon email for task {TaskId}", task.Id);
            }
        }

        public async Task NotifyTaskOverdueAsync(int userId, TaskItem task)
        {
            var title = "Task Overdue";
            var message = $"Your task '{task.Title}' is overdue";

            await CreateAsync(userId, title, message, NotificationType.TaskOverdue, NotificationPriority.Critical,
                $"/tasks/{task.Id}", task.Id);

            // Send email notification
            try
            {
                var user = await GetUserById(userId);
                if (user != null)
                {
                    var taskUrl = $"{GetBaseUrl()}/tasks/{task.Id}";
                    await _emailService.SendTaskOverdueEmailAsync(user.Email, user.FirstName, task.Title, taskUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send overdue email for task {TaskId}", task.Id);
            }
        }

        public async Task NotifyTaskCompletedAsync(int userId, TaskItem task)
        {
            var title = "Task Completed";
            var message = $"Task '{task.Title}' has been marked as completed";

            await CreateAsync(userId, title, message, NotificationType.TaskCompleted, NotificationPriority.Low,
                $"/tasks/{task.Id}", task.Id);
        }

        public async Task NotifyReminderFiredAsync(int userId, TaskItem task, DateTime fireTime)
        {
            var title = "Task Reminder";
            var message = $"Reminder: {task.Title} at {fireTime:hh:mm tt}";

            await CreateAsync(userId, title, message, NotificationType.ReminderFired, NotificationPriority.High,
                $"/tasks/{task.Id}", task.Id);

            // Send email notification
            try
            {
                var user = await GetUserById(userId);
                if (user != null)
                {
                    var taskUrl = $"{GetBaseUrl()}/tasks/{task.Id}";
                    await _emailService.SendTaskReminderEmailAsync(user.Email, user.FirstName, task.Title, fireTime, taskUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder email for task {TaskId}", task.Id);
            }
        }

        public async Task NotifyWelcomeAsync(int userId, string firstName)
        {
            var title = "Welcome to Task Flow!";
            var message = $"Welcome {firstName}! Your account has been created successfully.";

            await CreateAsync(userId, title, message, NotificationType.AccountWelcome, NotificationPriority.Medium);
        }

        public async Task NotifyProfileUpdatedAsync(int userId)
        {
            var title = "Profile Updated";
            var message = "Your profile has been updated successfully.";

            await CreateAsync(userId, title, message, NotificationType.AccountProfileUpdated, NotificationPriority.Low);
        }

        // Query methods
        public async Task<IEnumerable<NotificationDto>> GetForUserAsync(int userId, int page, int pageSize)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId, page, pageSize);
            var dtos = _mapper.Map<IEnumerable<NotificationDto>>(notifications);

            // Compute TimeAgo for each notification
            foreach (var dto in dtos)
            {
                dto.TimeAgo = GetTimeAgo(dto.CreatedAt);
            }

            return dtos;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            await _notificationRepository.MarkAsReadAsync(notificationId, userId);

            // Update unread count via SignalR
            var newCount = await _notificationRepository.GetUnreadCountAsync(userId);
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("UnreadCount", newCount);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);

            // Update unread count via SignalR
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("UnreadCount", 0);
        }

        public async Task DeleteAsync(int notificationId, int userId)
        {
            await _notificationRepository.DeleteAsync(notificationId, userId);

            // Update unread count via SignalR
            var newCount = await _notificationRepository.GetUnreadCountAsync(userId);
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("UnreadCount", newCount);
        }

        public async Task DeleteAllAsync(int userId)
        {
            // Remove all notifications for the user
            var notifications = await _notificationRepository.GetUserNotificationsAsync(userId);
            foreach (var n in notifications)
            {
                _notificationRepository.Remove(n);
            }
            await _notificationRepository.SaveChangesAsync();

            // Update unread count via SignalR
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("UnreadCount", 0);
        }

        public async Task<NotificationDto?> GetByIdAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification == null) return null;

            var dto = _mapper.Map<NotificationDto>(notification);
            dto.TimeAgo = GetTimeAgo(notification.CreatedAt);
            return dto;
        }

        // Helper methods
        private static string GetTimeAgo(DateTime createdAt)
        {
            var diff = DateTime.UtcNow - createdAt;
            if (diff.TotalSeconds < 60) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return createdAt.ToString("MMM d");
        }

        private async Task<AppUser?> GetUserById(int userId)
        {
            try
            {
                return await _userRepository.GetByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user {UserId}", userId);
                return null;
            }
        }

        private string GetBaseUrl()
        {
            return _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5247";
        }
    }
}
