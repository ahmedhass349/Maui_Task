using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Chatbot;
using Maui_Task.Shared.DTOs.Messages;
using Maui_Task.Shared.DTOs.Projects;
using Maui_Task.Shared.DTOs.Settings;
using Maui_Task.Shared.DTOs.Tasks;
using Maui_Task.Shared.DTOs.Teams;
using Maui_Task.Shared.Helpers;

namespace Maui_Task.Shared.Services
{
    public class SyncQueueService : ISyncQueueService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly AppDbContext _db;
        private readonly HttpApiService _api;

        public SyncQueueService(AppDbContext db, HttpApiService api)
        {
            _db = db;
            _api = api;
        }

        public async Task EnqueueAsync(string entityName, string operation, object payload)
        {
            _db.SyncQueueItems.Add(new SyncQueueItem
            {
                EntityName = entityName,
                Operation = operation,
                PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task ProcessPendingAsync()
        {
            var pending = await _db.SyncQueueItems
                .OrderBy(i => i.CreatedAt)
                .Take(25)
                .ToListAsync();

            foreach (var item in pending)
            {
                try
                {
                    item.LastAttemptAt = DateTime.UtcNow;
                    await ProcessItemAsync(item);
                    _db.SyncQueueItems.Remove(item);
                    await _db.SaveChangesAsync();
                }
                catch (HttpRequestException ex)
                {
                    item.RetryCount += 1;
                    item.LastError = ex.Message;
                    await _db.SaveChangesAsync();
                    break;
                }
                catch (Exception ex)
                {
                    item.RetryCount += 1;
                    item.LastError = ex.Message;
                    await _db.SaveChangesAsync();
                }
            }
        }

        private async Task ProcessItemAsync(SyncQueueItem item)
        {
            if (item.EntityName.Equals("Task", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessTaskItemAsync(item);
                return;
            }

            if (item.EntityName.Equals("Project", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessProjectItemAsync(item);
                return;
            }

            if (item.EntityName.Equals("Settings", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessSettingsItemAsync(item);
                return;
            }

            if (item.EntityName.Equals("Notification", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessNotificationItemAsync(item);
                return;
            }

            if (item.EntityName.Equals("Message", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessMessageItemAsync(item);
                return;
            }

            if (item.EntityName.Equals("Chatbot", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessChatbotItemAsync(item);
                return;
            }

            if (item.EntityName.Equals("Team", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessTeamItemAsync(item);
            }
        }

        private async Task ProcessTaskItemAsync(SyncQueueItem item)
        {
            switch (item.Operation.ToLowerInvariant())
            {
                case "create":
                    var createTaskPayload = TryDeserialize<TaskSyncPayload>(item.PayloadJson);
                    if (createTaskPayload is not null)
                    {
                        await _api.PostAsync<ApiResponse<TaskDto>>("/api/tasks", createTaskPayload.Request);
                        break;
                    }

                    await _api.PostAsync<ApiResponse<TaskDto>>("/api/tasks", Deserialize<CreateTaskRequest>(item.PayloadJson));
                    break;
                case "update":
                    var updatePayload = Deserialize<TaskSyncPayload>(item.PayloadJson);
                    await _api.PutAsync<ApiResponse<TaskDto>>($"/api/tasks/{updatePayload.Id}", updatePayload.Request);
                    break;
                case "delete":
                    await _api.DeleteAsync($"/api/tasks/{Deserialize<TaskIdSyncPayload>(item.PayloadJson).Id}");
                    break;
                case "status":
                    var statusPayload = Deserialize<TaskStatusSyncPayload>(item.PayloadJson);
                    await _api.PatchAsync<ApiResponse<TaskDto>>($"/api/tasks/{statusPayload.Id}/status", new UpdateStatusRequest { Status = statusPayload.Status });
                    break;
                case "star":
                    await _api.PatchAsync<ApiResponse<TaskDto>>($"/api/tasks/{Deserialize<TaskIdSyncPayload>(item.PayloadJson).Id}/star", null);
                    break;
            }
        }

        private async Task ProcessProjectItemAsync(SyncQueueItem item)
        {
            switch (item.Operation.ToLowerInvariant())
            {
                case "create":
                    var createProjectPayload = TryDeserialize<ProjectSyncPayload>(item.PayloadJson);
                    if (createProjectPayload is not null)
                    {
                        await _api.PostAsync<ApiResponse<ProjectDto>>("/api/projects", createProjectPayload.Request);
                        break;
                    }

                    await _api.PostAsync<ApiResponse<ProjectDto>>("/api/projects", Deserialize<CreateProjectRequest>(item.PayloadJson));
                    break;
                case "update":
                    var updatePayload = Deserialize<ProjectSyncPayload>(item.PayloadJson);
                    await _api.PutAsync<ApiResponse<ProjectDto>>($"/api/projects/{updatePayload.Id}", updatePayload.Request);
                    break;
                case "delete":
                    await _api.DeleteAsync($"/api/projects/{Deserialize<ProjectIdSyncPayload>(item.PayloadJson).Id}");
                    break;
            }
        }

        private async Task ProcessSettingsItemAsync(SyncQueueItem item)
        {
            switch (item.Operation.ToLowerInvariant())
            {
                case "profile":
                    await _api.PutAsync<ApiResponse<string>>("/api/settings/profile", Deserialize<SettingsProfileSyncPayload>(item.PayloadJson).Request);
                    break;
                case "password":
                    await _api.PutAsync<ApiResponse<string>>("/api/settings/password", Deserialize<SettingsPasswordSyncPayload>(item.PayloadJson).Request);
                    break;
                case "delete-account":
                    await _api.DeleteAsync("/api/settings/account");
                    break;
            }
        }

        private async Task ProcessNotificationItemAsync(SyncQueueItem item)
        {
            switch (item.Operation.ToLowerInvariant())
            {
                case "read":
                    await _api.PatchAsync<ApiResponse<string>>($"/api/notifications/{Deserialize<NotificationIdSyncPayload>(item.PayloadJson).Id}/read", null);
                    break;
                case "read-all":
                    await _api.PatchAsync<ApiResponse<string>>("/api/notifications/read-all", null);
                    break;
                case "delete":
                    await _api.DeleteAsync($"/api/notifications/{Deserialize<NotificationIdSyncPayload>(item.PayloadJson).Id}");
                    break;
                case "delete-all":
                    await _api.DeleteAsync("/api/notifications");
                    break;
            }
        }

        private async Task ProcessMessageItemAsync(SyncQueueItem item)
        {
            if (item.Operation.Equals("send", StringComparison.OrdinalIgnoreCase))
            {
                await _api.PostAsync<ApiResponse<MessageDto>>("/api/messages", Deserialize<MessageSyncPayload>(item.PayloadJson).Request);
            }
        }

        private async Task ProcessChatbotItemAsync(SyncQueueItem item)
        {
            switch (item.Operation.ToLowerInvariant())
            {
                case "create":
                    await _api.PostAsync<ApiResponse<ConversationDto>>("/api/chatbot/conversations", Deserialize<ChatbotConversationSyncPayload>(item.PayloadJson).Request);
                    break;
                case "send":
                    var messagePayload = Deserialize<ChatbotMessageSyncPayload>(item.PayloadJson);
                    await _api.PostAsync<ApiResponse<ChatbotMessageDto>>($"/api/chatbot/conversations/{messagePayload.ConversationId}/messages", messagePayload.Request);
                    break;
                case "delete":
                    await _api.DeleteAsync($"/api/chatbot/conversations/{Deserialize<ChatbotConversationIdSyncPayload>(item.PayloadJson).Id}");
                    break;
            }
        }

        private async Task ProcessTeamItemAsync(SyncQueueItem item)
        {
            switch (item.Operation.ToLowerInvariant())
            {
                case "create":
                    await _api.PostAsync<TeamDto>("/api/teams", Deserialize<TeamSyncPayload>(item.PayloadJson).Team);
                    break;
                case "update":
                    var updatePayload = Deserialize<TeamSyncPayload>(item.PayloadJson);
                    await _api.PutAsync<TeamDto>($"/api/teams/{updatePayload.Id}", updatePayload.Team);
                    break;
            }
        }

        private static T Deserialize<T>(string payloadJson) => JsonSerializer.Deserialize<T>(payloadJson, JsonOptions)!;

        private static T? TryDeserialize<T>(string payloadJson) where T : class
        {
            try
            {
                return JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
