using Maui_Task.Web.Services;
using Maui_Task.Web.Components;
using Maui_Task.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Web.Extensions;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add device-specific services used by the Maui_Task.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// HTTP client and shared services for TaskFlow API
// Resolve API base address from the current request host for server-side components.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<System.Net.Http.HttpClient>(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = accessor.HttpContext?.Request;
    var fallbackBase = builder.Configuration["AppSettings:ApiBaseUrl"] ?? "http://localhost:5247";
    var baseAddress = request is null
        ? fallbackBase
        : $"{request.Scheme}://{request.Host}";

    return new System.Net.Http.HttpClient { BaseAddress = new Uri(baseAddress) };
});
builder.Services.AddScoped<Maui_Task.Shared.Services.HttpApiService>(sp =>
{
    var client = sp.GetRequiredService<System.Net.Http.HttpClient>();
    var auth = sp.GetRequiredService<Maui_Task.Shared.Services.AuthenticationService>();
    return new Maui_Task.Shared.Services.HttpApiService(client, auth);
});

// Server token storage (cookie-based) and auth services
builder.Services.AddScoped<Maui_Task.Shared.Services.ITokenStorage, Maui_Task.Web.Services.ServerTokenStorage>();
builder.Services.AddScoped<Maui_Task.Shared.Services.AuthenticationService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.SignalRService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.NotificationContext>();
builder.Services.AddScoped<Maui_Task.Shared.Services.ITaskDataService, Maui_Task.Shared.Services.TaskDataService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.IProjectDataService, Maui_Task.Shared.Services.ProjectDataService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.IDashboardDataService, Maui_Task.Shared.Services.DashboardDataService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.ISettingsDataService, Maui_Task.Shared.Services.SettingsDataService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.INotificationDataService, Maui_Task.Shared.Services.NotificationDataService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.IMessageDataService, Maui_Task.Shared.Services.MessageDataService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.IChatbotDataService, Maui_Task.Shared.Services.ChatbotDataService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.ITeamDataService, Maui_Task.Shared.Services.TeamDataService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.ISyncQueueService, Maui_Task.Shared.Services.SyncQueueService>();

// Blazor auth
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<TaskFlowAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<TaskFlowAuthStateProvider>());

// Database
builder.Services.AddSingleton<Maui_Task.Shared.Services.Interfaces.IDatabasePathProvider, Maui_Task.Web.Services.WebDatabasePathProvider>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var provider = sp.GetRequiredService<Maui_Task.Shared.Services.Interfaces.IDatabasePathProvider>();
    options.UseSqlite($"Data Source={provider.GetDatabasePath()}");
});

builder.Services.AddTaskFlowServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseResponseCompression();

app.UseAntiforgery();

app.UseMiddleware<Maui_Task.Web.Middleware.ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
    c.RoutePrefix = "swagger"; 
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<Maui_Task.Web.Hubs.NotificationHub>("/hubs/notifications");

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(Maui_Task.Shared._Imports).Assembly);

// Initialize database for Web project
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
catch
{
    // ignore
}

app.Run();

public partial class Program
{
}
