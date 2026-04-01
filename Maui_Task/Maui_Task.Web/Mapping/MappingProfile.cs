// FILE: Mapping/MappingProfile.cs
// STATUS: UPDATED
// CHANGES: Added TaskComment mappings (#21), DueDateLabel for TaskDto (#28),
//          ProfileDto FirstName/LastName auto-mapped by convention (#24)

using System.Globalization;
using System.Linq;
using AutoMapper;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Calendar;
using Maui_Task.Shared.DTOs.Chatbot;
using Maui_Task.Shared.DTOs.Messages;
using Maui_Task.Shared.DTOs.Notifications;
using Maui_Task.Shared.DTOs.Projects;
using Maui_Task.Shared.DTOs.Settings;
using Maui_Task.Shared.DTOs.TaskComments;
using Maui_Task.Shared.DTOs.Tasks;
using Maui_Task.Shared.DTOs.Teams;

namespace Maui_Task.Web.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ─── 1. AppUser → UserDto ───────────────────────────────────────
            CreateMap<AppUser, UserDto>();

            // ─── 2. AppUser → ProfileDto ────────────────────────────────────
            // FirstName, LastName, FullName all auto-mapped by convention (#24)
            CreateMap<AppUser, ProfileDto>();

            // ─── 3. TaskItem → TaskDto ──────────────────────────────────────
            CreateMap<TaskItem, TaskDto>()
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project != null ? src.Project.Name : string.Empty))
                .ForMember(dest => dest.AssigneeName, opt => opt.MapFrom(src => src.Assignee != null ? src.Assignee.FullName : null))
                .ForMember(dest => dest.DueDateLabel, opt => opt.MapFrom(src =>
                    src.DueDate.HasValue ? src.DueDate.Value.ToString("MMM d", CultureInfo.InvariantCulture) : null));

            // ─── 4. CreateTaskRequest → TaskItem ────────────────────────────
            CreateMap<CreateTaskRequest, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsStarred, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Project, opt => opt.Ignore())
                .ForMember(dest => dest.Assignee, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore());

            // ─── 5. Project → ProjectDto ────────────────────────────────────
            CreateMap<Project, ProjectDto>()
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.Owner.FullName))
                .ForMember(dest => dest.TasksTotal, opt => opt.MapFrom(src => src.Tasks.Count))
                .ForMember(dest => dest.TasksCompleted, opt => opt.MapFrom(src => src.Tasks.Count(t => t.Status == Maui_Task.Shared.Data.Entities.TaskStatus.Completed)))
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count));

            // ─── 6. CalendarEvent → CalendarEventDto ────────────────────────
            CreateMap<CalendarEvent, CalendarEventDto>();

            // ─── 7. CreateCalendarEventRequest → CalendarEvent ──────────────
            CreateMap<CreateCalendarEventRequest, CalendarEvent>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Owner, opt => opt.Ignore());

            // ─── 8. Message → MessageDto ────────────────────────────────────
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender.FullName));

            // ─── 9. Notification → NotificationDto ──────────────────────────
            CreateMap<Notification, NotificationDto>();

            // ─── 10. ChatbotConversation → ConversationDto ──────────────────
            CreateMap<ChatbotConversation, ConversationDto>()
                .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages));

            // ─── 11. ChatbotConversation → ConversationListDto ──────────────
            CreateMap<ChatbotConversation, ConversationListDto>();

            // ─── 12. ChatbotMessage → ChatbotMessageDto ─────────────────────
            CreateMap<ChatbotMessage, ChatbotMessageDto>();

            // ─── 13. Team → TeamDto ─────────────────────────────────────────
            CreateMap<Team, TeamDto>()
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.Owner.FullName))
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count));

            // ─── 14. TaskComment → TaskCommentDto (#21) ─────────────────────
            CreateMap<TaskComment, TaskCommentDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.FullName));

            // ─── 15. CreateTaskCommentRequest → TaskComment (#21) ────────────
            CreateMap<CreateTaskCommentRequest, TaskComment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TaskId, opt => opt.Ignore())
                .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.Author, opt => opt.Ignore());
        }
    }
}
