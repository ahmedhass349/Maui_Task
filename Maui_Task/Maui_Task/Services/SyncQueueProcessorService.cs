using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Maui_Task.Shared.Services;

namespace Maui_Task.Services
{
    public class SyncQueueProcessorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SyncQueueProcessorService> _logger;

        public SyncQueueProcessorService(IServiceScopeFactory scopeFactory, ILogger<SyncQueueProcessorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var queue = scope.ServiceProvider.GetRequiredService<ISyncQueueService>();
                    await queue.ProcessPendingAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Sync queue processing skipped or failed.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
