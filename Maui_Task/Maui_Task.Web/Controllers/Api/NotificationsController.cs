using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/NotificationsController.cs
// STATUS: MODIFIED
// CHANGES: Fixed GetUserId() (#3), removed try-catch (#15), pass userId to MarkAsRead (#2), cleaned usings (#17), standardized route (#20)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// API controller for managing user notifications.
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Retrieves notifications for the authenticated user with pagination.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetForUserAsync(userId, page, pageSize);
            return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(notifications));
        }

        /// <summary>
        /// Gets the unread count for the authenticated user.
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(ApiResponse<int>.Ok(count));
        }

        /// <summary>
        /// Marks a specific notification as read.
        /// </summary>
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(ApiResponse<string>.Ok("Notification marked as read."));
        }

        /// <summary>
        /// Marks all notifications as read for the authenticated user.
        /// </summary>
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(ApiResponse<string>.Ok("All notifications marked as read."));
        }

        /// <summary>
        /// Gets a single notification by id for the authenticated user.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotification(int id)
        {
            var userId = GetUserId();
            var notification = await _notificationService.GetByIdAsync(id, userId);
            if (notification == null) return NotFound(ApiResponse<string>.Fail("Notification not found."));
            return Ok(ApiResponse<NotificationDto>.Ok(notification));
        }

        /// <summary>
        /// Deletes a specific notification for the authenticated user.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = GetUserId();
            await _notificationService.DeleteAsync(id, userId);
            return Ok(ApiResponse<string>.Ok("Notification deleted."));
        }

        /// <summary>
        /// Deletes all notifications for the authenticated user.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            var userId = GetUserId();
            await _notificationService.DeleteAllAsync(userId);
            return Ok(ApiResponse<string>.Ok("All notifications deleted."));
        }
    }
}
