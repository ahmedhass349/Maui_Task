using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Services
{
    public class ReminderService : IReminderService
    {
        private readonly IReminderRepository _reminderRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(
            IReminderRepository reminderRepository,
            INotificationService notificationService,
            ILogger<ReminderService> logger)
        {
            _reminderRepository = reminderRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task SaveRemindersAsync(CreateReminderDto dto, int userId)
        {
            try
            {
                // Delete existing reminders for this task first
                await _reminderRepository.DeleteByTaskIdAsync(dto.TaskId);

                var reminders = new List<Reminder>();

                // Parse the ReminderMap dictionary
                foreach (var kvp in dto.ReminderMap)
                {
                    var dateKey = kvp.Key; // "YYYY-MM-DD"
                    var timeStrings = kvp.Value; // ["9:00 AM", "2:00 PM"]

                    if (!DateTime.TryParseExact(dateKey, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
                    {
                        _logger.LogWarning("Invalid date format in ReminderMap: {DateKey}", dateKey);
                        continue;
                    }

                    foreach (var timeString in timeStrings)
                    {
                        if (DateTime.TryParse(timeString, out var time))
                        {
                            // Combine date and time
                            var fireAt = date.Date + time.TimeOfDay;

                            // Only save if the DateTime is in the future
                            if (fireAt > DateTime.UtcNow)
                            {
                                reminders.Add(new Reminder
                                {
                                    TaskId = dto.TaskId,
                                    UserId = userId,
                                    FireAt = fireAt,
                                    NotifyEmail = dto.NotifyEmail,
                                    NotifyInApp = dto.NotifyInApp,
                                    HasFired = false
                                });
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Invalid time format in ReminderMap: {TimeString}", timeString);
                        }
                    }
                }

                if (reminders.Any())
                {
                    await _reminderRepository.CreateBatchAsync(reminders);
                    _logger.LogInformation("Created {Count} reminders for task {TaskId}", reminders.Count, dto.TaskId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving reminders for task {TaskId}", dto.TaskId);
                throw;
            }
        }

        public async Task ProcessPendingRemindersAsync()
        {
            try
            {
                var pendingReminders = await _reminderRepository.GetPendingAsync(DateTime.UtcNow);

                foreach (var reminder in pendingReminders)
                {
                    try
                    {
                        // Fetch the associated task
                        var task = reminder.Task;
                        if (task != null)
                        {
                            // Create notification
                            await _notificationService.NotifyReminderFiredAsync(
                                reminder.UserId,
                                task,
                                reminder.FireAt);

                            // Mark the reminder as fired
                            await _reminderRepository.MarkFiredAsync(reminder.Id);

                            _logger.LogInformation("Processed reminder {ReminderId} for task {TaskId}",
                                reminder.Id, task.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Task not found for reminder {ReminderId}", reminder.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing reminder {ReminderId}", reminder.Id);
                    }
                }

                if (pendingReminders.Any())
                {
                    _logger.LogInformation("Processed {Count} pending reminders", pendingReminders.Count());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending reminders");
                throw;
            }
        }

        public async Task DeleteRemindersForTaskAsync(int taskId)
        {
            try
            {
                await _reminderRepository.DeleteByTaskIdAsync(taskId);
                _logger.LogInformation("Deleted all reminders for task {TaskId}", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reminders for task {TaskId}", taskId);
                throw;
            }
        }
    }
}
