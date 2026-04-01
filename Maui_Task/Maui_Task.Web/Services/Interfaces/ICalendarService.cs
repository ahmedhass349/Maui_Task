using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Calendar;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface ICalendarService
    {
        Task<IEnumerable<CalendarEventDto>> GetEventsAsync(int userId, DateTime? from, DateTime? to);
        Task<CalendarEventDto> CreateEventAsync(int userId, CreateCalendarEventRequest request);
        Task<CalendarEventDto> UpdateEventAsync(int userId, int eventId, UpdateCalendarEventRequest request);
        Task DeleteEventAsync(int userId, int eventId);
    }
}
