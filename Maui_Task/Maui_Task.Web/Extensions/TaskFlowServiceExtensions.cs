/*
    CLEANUP [Maui_Task.Web/Extensions/TaskFlowServiceExtensions.cs]:
    - Removed: Hardcoded JWT fallback literals and hardcoded CORS origin literals.
    - Fixed: JWT and CORS wiring now read from configuration values.
    - Moved: none
*/

using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Newtonsoft.Json;
using Maui_Task.Web.Repositories;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services;
using Maui_Task.Web.Services.Interfaces;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Mapping;
using System;
using System.Linq;

namespace Maui_Task.Web.Extensions
{
    public static class TaskFlowServiceExtensions
    {
        public static IServiceCollection AddTaskFlowServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ── AutoMapper ───────────────────────────────────────────────────
            services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);

            // ── JWT Authentication ───────────────────────────────────────────
            var jwtKey = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Missing required configuration: Jwt:Key");
            var jwtIssuer = configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Missing required configuration: Jwt:Issuer");
            var jwtAudience = configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Missing required configuration: Jwt:Audience");

            var configuredCorsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            var fallbackOrigin = configuration["AppSettings:BaseUrl"];
            var corsOrigins = (configuredCorsOrigins ?? Array.Empty<string>())
                .Concat(string.IsNullOrWhiteSpace(fallbackOrigin)
                    ? Array.Empty<string>()
                    : new[] { fallbackOrigin })
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                // Return JSON for auth failures instead of default plain text
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        var path = context.Request.Path;
                        var accept = context.Request.Headers.Accept.ToString();
                        var isApiOrHub = path.StartsWithSegments("/api") || path.StartsWithSegments("/hubs");
                        var expectsHtml = accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);

                        if (!isApiOrHub && expectsHtml)
                        {
                            var returnUrl = Uri.EscapeDataString($"{context.Request.Path}{context.Request.QueryString}");
                            context.Response.Redirect($"/login?returnUrl={returnUrl}");
                            return;
                        }

                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var errorResponse = new { success = false, message = "Unauthorized. Please log in to continue." };
                        var json = JsonConvert.SerializeObject(errorResponse);
                        await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        var errorResponse = new { success = false, message = "You do not have permission to perform this action." };
                        var json = JsonConvert.SerializeObject(errorResponse);
                        await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json));
                    },
                    // Read JWT from cookie for normal requests and from query string for SignalR hub connections
                    OnMessageReceived = context =>
                    {
                        var cookieToken = context.Request.Cookies["taskflow_token"];
                        if (!string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                            return Task.CompletedTask;
                        }

                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();

            // ── CORS ─────────────────────────────────────────────────────────
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.WithOrigins(corsOrigins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });

            // ── Controllers + JSON ───────────────────────────────────────────
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });

            // ── FluentValidation ─────────────────────────────────────────────
            services.AddFluentValidationAutoValidation();
            services.AddFluentValidationClientsideAdapters();
            services.AddValidatorsFromAssembly(typeof(TaskFlowServiceExtensions).Assembly);

            // ── Swagger ──────────────────────────────────────────────────────
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                        Array.Empty<string>()
                    }
                });
            });

            // ── Repositories (DI) ────────────────────────────────────────────
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IReminderRepository, ReminderRepository>();
            services.AddScoped<IChatbotRepository, ChatbotRepository>();
            services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();

            // ── Services (DI) ────────────────────────────────────────────────
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<ICalendarService, CalendarService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IReminderService, ReminderService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<IChatbotService, ChatbotService>();
            services.AddScoped<ITaskCommentService, TaskCommentService>();

            // ── SignalR ─────────────────────────────────────────────────────
            services.AddSignalR();

            // ── Background Services ───────────────────────────────────────────
            services.AddHostedService<BackgroundServices.ReminderProcessorService>();
            services.AddHostedService<BackgroundServices.DueDateWarningService>();

            // ── Helpers (DI) ─────────────────────────────────────────────────
            services.AddScoped<JwtHelper>();

            return services;
        }
    }
}
