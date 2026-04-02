/*
    CLEANUP [Maui_Task/MauiProgram.cs]:
    - Removed: none
    - Fixed: Added missing NotificationContext DI registration and replaced hardcoded API base URL with configuration-driven value.
    - Moved: none
*/

using Maui_Task.Services;
using Maui_Task.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Maui_Task
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the Maui_Task.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            // Register HTTP client + shared services for TaskFlow integration.
            // Normalize stale configs that still point to the old local port (5001).
            var configuredApiBaseUrl = builder.Configuration["AppSettings:ApiBaseUrl"];
            var apiBaseUrl = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
                ? "http://localhost:5247"
                : configuredApiBaseUrl.Trim();

            if (apiBaseUrl.Contains(":5001", StringComparison.OrdinalIgnoreCase))
            {
                apiBaseUrl = apiBaseUrl
                    .Replace("https://localhost:5001", "http://localhost:5247", StringComparison.OrdinalIgnoreCase)
                    .Replace("http://localhost:5001", "http://localhost:5247", StringComparison.OrdinalIgnoreCase);
            }

            var apiHttpClient = new System.Net.Http.HttpClient { BaseAddress = new Uri(apiBaseUrl) };
            builder.Services.AddSingleton<System.Net.Http.HttpClient>(apiHttpClient);

            // Repositories
            builder.Services.AddScoped<Maui_Task.Shared.Repositories.Interfaces.IUserRepository, Maui_Task.Shared.Repositories.UserRepository>();
            builder.Services.AddScoped<Maui_Task.Shared.Repositories.Interfaces.ITaskRepository, Maui_Task.Shared.Repositories.TaskRepository>();
            builder.Services.AddScoped<Maui_Task.Shared.Repositories.Interfaces.IProjectRepository, Maui_Task.Shared.Repositories.ProjectRepository>();
            builder.Services.AddScoped<Maui_Task.Shared.Repositories.Interfaces.INotificationRepository, Maui_Task.Shared.Repositories.NotificationRepository>();
            builder.Services.AddScoped<Maui_Task.Shared.Repositories.Interfaces.IReminderRepository, Maui_Task.Shared.Repositories.ReminderRepository>();

            builder.Services.AddScoped<Maui_Task.Shared.Services.IAuthService, Maui_Task.Shared.Services.LocalAuthService>();
            builder.Services.AddSingleton<Maui_Task.Shared.Services.AuthenticationService>(sp =>
                new Maui_Task.Shared.Services.AuthenticationService(
                    sp.GetRequiredService<System.Net.Http.HttpClient>(),
                    sp.GetRequiredService<Maui_Task.Shared.Services.ITokenStorage>()));
            builder.Services.AddSingleton<Maui_Task.Shared.Services.HttpApiService>(sp =>
                new Maui_Task.Shared.Services.HttpApiService(sp.GetRequiredService<System.Net.Http.HttpClient>(), sp.GetRequiredService<Maui_Task.Shared.Services.IAuthService>()));
            builder.Services.AddScoped<Maui_Task.Shared.Services.ITaskDataService, Maui_Task.Shared.Services.LocalTaskDataService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.ITaskService>(sp =>
                sp.GetRequiredService<Maui_Task.Shared.Services.ITaskDataService>());
            builder.Services.AddScoped<Maui_Task.Shared.Services.IProjectDataService, Maui_Task.Shared.Services.LocalProjectDataService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.IProjectService>(sp =>
                sp.GetRequiredService<Maui_Task.Shared.Services.IProjectDataService>());
            builder.Services.AddScoped<Maui_Task.Shared.Services.IDashboardDataService, Maui_Task.Shared.Services.DashboardDataService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.ISettingsDataService, Maui_Task.Shared.Services.LocalSettingsDataService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.INotificationDataService, Maui_Task.Shared.Services.LocalNotificationDataService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.IMessageDataService, Maui_Task.Shared.Services.MessageDataService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.IChatbotDataService, Maui_Task.Shared.Services.ChatbotDataService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.ITeamDataService, Maui_Task.Shared.Services.TeamDataService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.ISyncQueueService, Maui_Task.Shared.Services.SyncQueueService>();
            builder.Services.AddHostedService<Maui_Task.Services.SyncQueueProcessorService>();
            builder.Services.AddSingleton<Maui_Task.Shared.Services.SignalRService>(sp =>
                new Maui_Task.Shared.Services.SignalRService(
                    sp.GetRequiredService<Maui_Task.Shared.Services.IAuthService>()));
            builder.Services.AddScoped<Maui_Task.Shared.Services.NotificationContext>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.LocalNotificationClientService>();
            builder.Services.AddScoped<Maui_Task.Shared.Services.INotificationClientService>(sp =>
                sp.GetRequiredService<Maui_Task.Shared.Services.LocalNotificationClientService>());

            // MAUI secure token storage
            builder.Services.AddSingleton<Maui_Task.Shared.Services.ITokenStorage, Maui_Task.Services.MauiSecureTokenStorage>();

            // Blazor auth
            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddSingleton<AuthenticationStateProvider, TaskFlowAuthStateProvider>();
            builder.Services.AddSingleton<TaskFlowAuthStateProvider>();

            // Database
            builder.Services.AddSingleton<Maui_Task.Shared.Services.Interfaces.IDatabasePathProvider, Maui_Task.Services.MauiDatabasePathProvider>();
            builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
            {
                var provider = sp.GetRequiredService<Maui_Task.Shared.Services.Interfaces.IDatabasePathProvider>();
                options.UseSqlite($"Data Source={provider.GetDatabasePath()}");
            }, ServiceLifetime.Transient);

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
                builder.Services.AddBlazorWebViewDeveloperTools();
                builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            return app;
        }
    }
}
