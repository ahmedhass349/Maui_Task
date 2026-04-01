using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/ProjectsController.cs
// STATUS: MODIFIED
// CHANGES: Fixed GetUserId() (#3), removed try-catch (#15), pass userId to Update/Delete/ToggleStar (#2), cleaned usings (#17)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maui_Task.Shared.DTOs.Projects;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// Manages project operations including CRUD, starring, and member management.
    /// </summary>
    [ApiController]
    [Route("api/projects")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Retrieves all projects for the authenticated user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserProjects()
        {
            var userId = GetUserId();
            var projects = await _projectService.GetUserProjectsAsync(userId);
            return Ok(ApiResponse<IEnumerable<ProjectDto>>.Ok(projects));
        }

        /// <summary>
        /// Retrieves a single project by its identifier.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectById(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            return Ok(ApiResponse<ProjectDto>.Ok(project));
        }

        /// <summary>
        /// Creates a new project for the authenticated user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            var userId = GetUserId();
            var project = await _projectService.CreateProjectAsync(userId, request);
            return StatusCode(201, ApiResponse<ProjectDto>.Ok(project, "Project created successfully"));
        }

        /// <summary>
        /// Updates an existing project by its identifier.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
        {
            var userId = GetUserId();
            var project = await _projectService.UpdateProjectAsync(userId, id, request);
            return Ok(ApiResponse<ProjectDto>.Ok(project, "Project updated successfully"));
        }

        /// <summary>
        /// Deletes a project by its identifier.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = GetUserId();
            await _projectService.DeleteProjectAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Toggles the starred status of a project.
        /// </summary>
        [HttpPatch("{id}/star")]
        public async Task<IActionResult> ToggleStar(int id)
        {
            var userId = GetUserId();
            var project = await _projectService.ToggleStarAsync(userId, id);
            return Ok(ApiResponse<ProjectDto>.Ok(project, "Project star toggled"));
        }

        /// <summary>
        /// Retrieves the members of a project.
        /// </summary>
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetProjectMembers(int id)
        {
            var members = await _projectService.GetProjectMembersAsync(id);
            return Ok(ApiResponse<IEnumerable<ProjectMemberDto>>.Ok(members));
        }
    }
}
