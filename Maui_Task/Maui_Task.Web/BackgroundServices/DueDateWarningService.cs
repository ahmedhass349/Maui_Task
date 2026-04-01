using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Web.Services.Interfaces;
using TaskStatus = Maui_Task.Shared.Data.Entities.TaskStatus;

namespace Maui_Task.Web.BackgroundServices
{
    public class DueDateWarningService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DueDateWarningService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);
        // map notification key -> time sent, used for dedup and cleanup
        private readonly Dictionary<string, DateTime> _sentNotifications = new Dictionary<string, DateTime>();
        private readonly object _sentNotificationsLock = new object();

        public DueDateWarningService(IServiceProvider serviceProvider, ILogger<DueDateWarningService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Due date warning service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    await ProcessDueDateWarnings(dbContext, notificationService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing due date warnings");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Due date warning service stopped");
        }

        private async Task ProcessDueDateWarnings(AppDbContext dbContext, INotificationService notificationService)
        {
            var now = DateTime.UtcNow;

            // 24 hour warnings
            var tasksDueIn24Hours = await dbContext.TaskItems
                .Include(t => t.Assignee)
                .Where(t => t.AssigneeId.HasValue &&
                           t.DueDate.HasValue &&
                           t.DueDate.Value > now &&
                           t.DueDate.Value <= now.AddHours(24) &&
                           t.Status != TaskStatus.Completed)
                .ToListAsync();

            foreach (var task in tasksDueIn24Hours)
            {
                var notificationKey = $"24h-{task.Id}-{task.AssigneeId}";
                var shouldSend = false;
                lock (_sentNotificationsLock)
                {
                    if (!_sentNotifications.ContainsKey(notificationKey))
                    {
                        _sentNotifications[notificationKey] = DateTime.UtcNow;
                        shouldSend = true;
                    }
                }

                if (shouldSend)
                {
                    await notificationService.NotifyTaskDueSoonAsync(task.AssigneeId!.Value, task, "24 hours");
                    _logger.LogInformation("Sent 24-hour due warning for task {TaskId}", task.Id);
                }
            }

            // 1 hour warnings
            var tasksDueIn1Hour = await dbContext.TaskItems
                .Include(t => t.Assignee)
                .Where(t => t.AssigneeId.HasValue &&
                           t.DueDate.HasValue &&
                           t.DueDate.Value > now &&
                           t.DueDate.Value <= now.AddHours(1) &&
                           t.Status != TaskStatus.Completed)
                .ToListAsync();

            foreach (var task in tasksDueIn1Hour)
            {
                var notificationKey = $"1h-{task.Id}-{task.AssigneeId}";
                var shouldSend = false;
                lock (_sentNotificationsLock)
                {
                    if (!_sentNotifications.ContainsKey(notificationKey))
                    {
                        _sentNotifications[notificationKey] = DateTime.UtcNow;
                        shouldSend = true;
                    }
                }

                if (shouldSend)
                {
                    await notificationService.NotifyTaskDueSoonAsync(task.AssigneeId!.Value, task, "1 hour");
                    _logger.LogInformation("Sent 1-hour due warning for task {TaskId}", task.Id);
                }
            }

            // Overdue tasks
            var overdueTasks = await dbContext.TaskItems
                .Include(t => t.Assignee)
                .Where(t => t.AssigneeId.HasValue &&
                           t.DueDate.HasValue &&
                           t.DueDate.Value < now &&
                           t.Status != TaskStatus.Completed)
                .ToListAsync();

            foreach (var task in overdueTasks)
            {
                var notificationKey = $"overdue-{task.Id}-{task.AssigneeId}";
                var shouldSend = false;
                lock (_sentNotificationsLock)
                {
                    if (!_sentNotifications.ContainsKey(notificationKey))
                    {
                        _sentNotifications[notificationKey] = DateTime.UtcNow;
                        shouldSend = true;
                    }
                }

                if (shouldSend)
                {
                    await notificationService.NotifyTaskOverdueAsync(task.AssigneeId!.Value, task);
                    _logger.LogInformation("Sent overdue notification for task {TaskId}", task.Id);
                }
            }

            // Clean old notifications (older than 7 days)
            var cutoffDate = now.AddDays(-7);
            List<string> oldKeys;
            lock (_sentNotificationsLock)
            {
                oldKeys = _sentNotifications.Where(kvp => kvp.Value < cutoffDate)
                                            .Select(kvp => kvp.Key)
                                            .ToList();

                foreach (var oldKey in oldKeys)
                {
                    _sentNotifications.Remove(oldKey);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Due date warning service stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}
