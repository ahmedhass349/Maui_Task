using Maui_Task.Shared.Services;
using Maui_Task.Web.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the Maui_Task.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// HTTP client and shared services for TaskFlow API
// Blazor WASM: create a single HttpClient instance pointing to the host base address
var httpClient = new System.Net.Http.HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
builder.Services.AddSingleton<System.Net.Http.HttpClient>(httpClient);
builder.Services.AddScoped<Maui_Task.Shared.Services.HttpApiService>(sp =>
{
	var client = sp.GetRequiredService<System.Net.Http.HttpClient>();
	var auth = sp.GetRequiredService<Maui_Task.Shared.Services.AuthenticationService>();
	return new Maui_Task.Shared.Services.HttpApiService(client, auth);
});

// WASM requires JS runtime for storage; use scoped services
builder.Services.AddScoped<Maui_Task.Shared.Services.ITokenStorage, Maui_Task.Web.Client.Services.WasmTokenStorage>();
builder.Services.AddScoped<Maui_Task.Shared.Services.AuthenticationService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.SignalRService>();
builder.Services.AddScoped<Maui_Task.Shared.Services.NotificationContext>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<TaskFlowAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<TaskFlowAuthStateProvider>());

var host = builder.Build();

// Initialize persisted token for WASM
using (var scope = host.Services.CreateScope())
{
	var auth = scope.ServiceProvider.GetRequiredService<Maui_Task.Shared.Services.AuthenticationService>();
	await auth.InitializeAsync();
}

await host.RunAsync();
