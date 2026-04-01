// FILE: Services/CalendarService.cs
// STATUS: UPDATED
// CHANGES: Added userId ownership checks to Update/Delete (#2),
//          Fixed GetEventsAsync to filter in DB query instead of in-memory (#7)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Calendar;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly IGenericRepository<CalendarEvent> _calendarRepository;
        private readonly IMapper _mapper;

        public CalendarService(IGenericRepository<CalendarEvent> calendarRepository, IMapper mapper)
        {
            _calendarRepository = calendarRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CalendarEventDto>> GetEventsAsync(int userId, DateTime? from, DateTime? to)
        {
            // Fix #7: Filter dates in DB query instead of in-memory
            var query = _calendarRepository.Query().Where(e => e.OwnerId == userId);

            if (from.HasValue)
            {
                query = query.Where(e => e.EndAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(e => e.StartAt <= to.Value);
            }

            var events = await query.OrderBy(e => e.StartAt).ToListAsync();

            return _mapper.Map<IEnumerable<CalendarEventDto>>(events);
        }

        public async Task<CalendarEventDto> CreateEventAsync(int userId, CreateCalendarEventRequest request)
        {
            var calendarEvent = new CalendarEvent
            {
                OwnerId = userId,
                Title = request.Title,
                Description = request.Description,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                Color = request.Color,
                MeetingLink = request.MeetingLink,
                CreatedAt = DateTime.UtcNow
            };

            await _calendarRepository.AddAsync(calendarEvent);
            await _calendarRepository.SaveChangesAsync();

            return _mapper.Map<CalendarEventDto>(calendarEvent);
        }

        public async Task<CalendarEventDto> UpdateEventAsync(int userId, int eventId, UpdateCalendarEventRequest request)
        {
            var calendarEvent = await _calendarRepository.GetByIdAsync(eventId);
            if (calendarEvent == null)
                throw new KeyNotFoundException($"Calendar event with ID {eventId} not found.");

            // Ownership check (#2)
            if (calendarEvent.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to update this event.");

            calendarEvent.Title = request.Title;
            calendarEvent.Description = request.Description;
            calendarEvent.StartAt = request.StartAt;
            calendarEvent.EndAt = request.EndAt;
            calendarEvent.Color = request.Color;
            calendarEvent.MeetingLink = request.MeetingLink;

            _calendarRepository.Update(calendarEvent);
            await _calendarRepository.SaveChangesAsync();

            return _mapper.Map<CalendarEventDto>(calendarEvent);
        }

        public async Task DeleteEventAsync(int userId, int eventId)
        {
            var calendarEvent = await _calendarRepository.GetByIdAsync(eventId);
            if (calendarEvent == null)
                throw new KeyNotFoundException($"Calendar event with ID {eventId} not found.");

            // Ownership check (#2)
            if (calendarEvent.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this event.");

            _calendarRepository.Remove(calendarEvent);
            await _calendarRepository.SaveChangesAsync();
        }
    }
}
