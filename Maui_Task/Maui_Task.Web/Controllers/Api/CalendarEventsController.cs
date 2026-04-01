using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/CalendarEventsController.cs
// STATUS: MODIFIED
// CHANGES: Fixed GetUserId() (#3), removed try-catch (#15), pass userId to Update/Delete (#2), cleaned usings (#17)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maui_Task.Shared.DTOs.Calendar;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// API controller for managing calendar events.
    /// </summary>
    [ApiController]
    [Route("api/calendar-events")]
    [Authorize]
    public class CalendarEventsController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarEventsController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Retrieves calendar events for the authenticated user, optionally filtered by date range.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var userId = GetUserId();
            var events = await _calendarService.GetEventsAsync(userId, from, to);
            return Ok(ApiResponse<IEnumerable<CalendarEventDto>>.Ok(events, "Events retrieved successfully"));
        }

        /// <summary>
        /// Creates a new calendar event for the authenticated user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateCalendarEventRequest request)
        {
            var userId = GetUserId();
            var calendarEvent = await _calendarService.CreateEventAsync(userId, request);
            return StatusCode(201, ApiResponse<CalendarEventDto>.Ok(calendarEvent, "Event created successfully"));
        }

        /// <summary>
        /// Updates an existing calendar event.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateCalendarEventRequest request)
        {
            var userId = GetUserId();
            var calendarEvent = await _calendarService.UpdateEventAsync(userId, id, request);
            return Ok(ApiResponse<CalendarEventDto>.Ok(calendarEvent, "Event updated successfully"));
        }

        /// <summary>
        /// Deletes a calendar event by its identifier.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = GetUserId();
            await _calendarService.DeleteEventAsync(userId, id);
            return NoContent();
        }
    }
}
