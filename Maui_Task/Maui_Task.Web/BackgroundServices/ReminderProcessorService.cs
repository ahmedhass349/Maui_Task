using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.BackgroundServices
{
    public class ReminderProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderProcessorService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public ReminderProcessorService(IServiceProvider serviceProvider, ILogger<ReminderProcessorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reminder processor started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
                    await reminderService.ProcessPendingRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing reminders");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Reminder processor stopped");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reminder processor stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}
