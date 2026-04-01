using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/TasksController.cs
// STATUS: MODIFIED
// CHANGES: Fixed GetUserId() (#3), removed try-catch (#15), pass userId to Update/Delete/ToggleStar/UpdateStatus (#2),
//          replaced inline UpdateStatusBody with UpdateStatusRequest DTO (#5), cleaned usings (#17)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Maui_Task.Shared.DTOs.Tasks;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// Manages task operations including CRUD, filtering, starring, and status updates.
    /// </summary>
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Retrieves a filtered list of tasks for the authenticated user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] TaskFilterRequest filter)
        {
            var userId = GetUserId();
            var tasks = await _taskService.GetTasksAsync(userId, filter);
            return Ok(ApiResponse<IEnumerable<TaskDto>>.Ok(tasks));
        }

        /// <summary>
        /// Retrieves a single task by its identifier.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            return Ok(ApiResponse<TaskDto>.Ok(task));
        }

        /// <summary>
        /// Creates a new task for the authenticated user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var userId = GetUserId();
            var task = await _taskService.CreateTaskAsync(userId, request);
            return StatusCode(201, ApiResponse<TaskDto>.Ok(task, "Task created successfully"));
        }

        /// <summary>
        /// Updates an existing task by its identifier.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
        {
            var userId = GetUserId();
            var task = await _taskService.UpdateTaskAsync(userId, id, request);
            return Ok(ApiResponse<TaskDto>.Ok(task, "Task updated successfully"));
        }

        /// <summary>
        /// Deletes a task by its identifier.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetUserId();
            await _taskService.DeleteTaskAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Toggles the starred status of a task.
        /// </summary>
        [HttpPatch("{id}/star")]
        public async Task<IActionResult> ToggleStar(int id)
        {
            var userId = GetUserId();
            var task = await _taskService.ToggleStarAsync(userId, id);
            return Ok(ApiResponse<TaskDto>.Ok(task, "Task star toggled"));
        }

        /// <summary>
        /// Updates the status of a task.
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var userId = GetUserId();
            var task = await _taskService.UpdateStatusAsync(userId, id, request.Status);
            return Ok(ApiResponse<TaskDto>.Ok(task, "Task status updated"));
        }
    }
}
