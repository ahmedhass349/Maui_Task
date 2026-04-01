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

            // Register HTTP client + shared services for TaskFlow integration
            var apiBaseUrl = builder.Configuration["AppSettings:ApiBaseUrl"] ?? "https://localhost:5001";
            var apiHttpClient = new System.Net.Http.HttpClient { BaseAddress = new Uri(apiBaseUrl) };
            builder.Services.AddSingleton<System.Net.Http.HttpClient>(apiHttpClient);

            builder.Services.AddSingleton<Maui_Task.Shared.Services.AuthenticationService>(sp =>
                new Maui_Task.Shared.Services.AuthenticationService(
                    sp.GetRequiredService<System.Net.Http.HttpClient>(),
                    sp.GetRequiredService<Maui_Task.Shared.Services.ITokenStorage>()));
            builder.Services.AddSingleton<Maui_Task.Shared.Services.HttpApiService>(sp =>
                new Maui_Task.Shared.Services.HttpApiService(sp.GetRequiredService<System.Net.Http.HttpClient>(), sp.GetRequiredService<Maui_Task.Shared.Services.AuthenticationService>()));
            builder.Services.AddSingleton<Maui_Task.Shared.Services.SignalRService>(sp =>
                new Maui_Task.Shared.Services.SignalRService(
                    sp.GetRequiredService<Maui_Task.Shared.Services.AuthenticationService>()));
            builder.Services.AddScoped<Maui_Task.Shared.Services.NotificationContext>();

            // MAUI secure token storage
            builder.Services.AddSingleton<Maui_Task.Shared.Services.ITokenStorage, Maui_Task.Services.MauiSecureTokenStorage>();

            // Blazor auth
            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddSingleton<AuthenticationStateProvider, TaskFlowAuthStateProvider>();
            builder.Services.AddSingleton<TaskFlowAuthStateProvider>();

            // Database
            builder.Services.AddSingleton<Maui_Task.Shared.Services.Interfaces.IDatabasePathProvider, Maui_Task.Services.MauiDatabasePathProvider>();
            builder.Services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var provider = sp.GetRequiredService<Maui_Task.Shared.Services.Interfaces.IDatabasePathProvider>();
                options.UseSqlite($"Data Source={provider.GetDatabasePath()}");
            });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Initialize persisted token (blocking during startup)
            try
            {
                var auth = app.Services.GetRequiredService<Maui_Task.Shared.Services.AuthenticationService>();
                auth.InitializeAsync().GetAwaiter().GetResult();

                var db = app.Services.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }
            catch
            {
                // ignore initialization errors
            }

            return app;
        }
    }
}
