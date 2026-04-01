using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(INotificationService notificationService, ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                // Send unread count on connect
                var count = await _notificationService.GetUnreadCountAsync(int.Parse(userId));
                await Clients.Caller.SendAsync("UnreadCount", count);

                _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
            }
            await base.OnConnectedAsync();
        }

        public async Task MarkAsRead(int notificationId)
        {
            var userId = int.Parse(Context.UserIdentifier!);
            await _notificationService.MarkAsReadAsync(notificationId, userId);
            var newCount = await _notificationService.GetUnreadCountAsync(userId);
            await Clients.Caller.SendAsync("UnreadCount", newCount);
        }

        public async Task MarkAllRead()
        {
            var userId = int.Parse(Context.UserIdentifier!);
            await _notificationService.MarkAllAsReadAsync(userId);
            await Clients.Caller.SendAsync("UnreadCount", 0);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
