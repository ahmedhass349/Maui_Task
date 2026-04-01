using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/TaskCommentsController.cs
// STATUS: NEW
// CHANGES: Full CRUD controller for TaskComments (#21)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maui_Task.Shared.DTOs.TaskComments;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// API controller for managing task comments.
    /// Comments are nested under tasks for retrieval/creation, but addressed directly for update/delete.
    /// </summary>
    [ApiController]
    [Authorize]
    public class TaskCommentsController : ControllerBase
    {
        private readonly ITaskCommentService _taskCommentService;

        public TaskCommentsController(ITaskCommentService taskCommentService)
        {
            _taskCommentService = taskCommentService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Retrieves all comments for a specific task.
        /// </summary>
        [HttpGet("api/tasks/{taskId}/comments")]
        public async Task<IActionResult> GetComments(int taskId)
        {
            var comments = await _taskCommentService.GetCommentsAsync(taskId);
            return Ok(ApiResponse<IEnumerable<TaskCommentDto>>.Ok(comments));
        }

        /// <summary>
        /// Creates a new comment on a specific task.
        /// </summary>
        [HttpPost("api/tasks/{taskId}/comments")]
        public async Task<IActionResult> CreateComment(int taskId, [FromBody] CreateTaskCommentRequest request)
        {
            var userId = GetUserId();
            var comment = await _taskCommentService.CreateCommentAsync(userId, taskId, request);
            return StatusCode(201, ApiResponse<TaskCommentDto>.Ok(comment, "Comment created successfully"));
        }

        /// <summary>
        /// Updates an existing comment by its identifier.
        /// </summary>
        [HttpPut("api/task-comments/{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateTaskCommentRequest request)
        {
            var userId = GetUserId();
            var comment = await _taskCommentService.UpdateCommentAsync(userId, id, request);
            return Ok(ApiResponse<TaskCommentDto>.Ok(comment, "Comment updated successfully"));
        }

        /// <summary>
        /// Deletes a comment by its identifier.
        /// </summary>
        [HttpDelete("api/task-comments/{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userId = GetUserId();
            await _taskCommentService.DeleteCommentAsync(userId, id);
            return NoContent();
        }
    }
}
