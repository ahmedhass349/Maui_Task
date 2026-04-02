using System.Collections.Generic;
using Maui_Task.Shared.DTOs.Chatbot;
using Maui_Task.Shared.DTOs.Messages;
using Maui_Task.Shared.DTOs.Projects;
using Maui_Task.Shared.DTOs.Settings;
using Maui_Task.Shared.DTOs.Tasks;
using Maui_Task.Shared.DTOs.Teams;

namespace Maui_Task.Shared.Services
{
    public sealed record TaskIdSyncPayload(int Id);
    public sealed record TaskStatusSyncPayload(int Id, string Status);
    public sealed record TaskSyncPayload(int Id, CreateTaskRequest Request);

    public sealed record ProjectIdSyncPayload(int Id);
    public sealed record ProjectSyncPayload(int Id, CreateProjectRequest Request);

    public sealed record SettingsProfileSyncPayload(UpdateProfileRequest Request);
    public sealed record SettingsPasswordSyncPayload(ChangePasswordRequest Request);

    public sealed record NotificationIdSyncPayload(int Id);

    public sealed record MessageSyncPayload(SendMessageRequest Request);

    public sealed record ChatbotConversationIdSyncPayload(int Id);
    public sealed record ChatbotConversationSyncPayload(CreateConversationRequest Request);
    public sealed record ChatbotMessageSyncPayload(int ConversationId, SendChatbotMessageRequest Request);

    public sealed record TeamSyncPayload(int Id, TeamDto Team);
}
