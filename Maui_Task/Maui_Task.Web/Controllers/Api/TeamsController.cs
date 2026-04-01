using Maui_Task.Shared.Helpers;
// FILE: Controllers/Api/TeamsController.cs
// STATUS: MODIFIED
// CHANGES: Fixed GetUserId() (#3), removed try-catch (#15), fixed null! (#6),
//          added PUT/DELETE team + DELETE member endpoints (#22), cleaned usings (#17), standardized route (#20)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maui_Task.Shared.DTOs.Teams;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Controllers.Api
{
    /// <summary>
    /// API controller for managing teams and team membership.
    /// </summary>
    [ApiController]
    [Route("api/teams")]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identity could not be determined.");
            return userId;
        }

        /// <summary>
        /// Retrieves all teams the authenticated user belongs to.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserTeams()
        {
            var userId = GetUserId();
            var teams = await _teamService.GetUserTeamsAsync(userId);
            return Ok(ApiResponse<IEnumerable<TeamDto>>.Ok(teams, "Teams retrieved successfully"));
        }

        /// <summary>
        /// Creates a new team owned by the authenticated user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
        {
            var userId = GetUserId();
            var team = await _teamService.CreateTeamAsync(userId, request);
            return StatusCode(201, ApiResponse<TeamDto>.Ok(team, "Team created successfully"));
        }

        /// <summary>
        /// Updates an existing team.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
        {
            var userId = GetUserId();
            var team = await _teamService.UpdateTeamAsync(userId, id, request);
            return Ok(ApiResponse<TeamDto>.Ok(team, "Team updated successfully"));
        }

        /// <summary>
        /// Deletes a team by its identifier.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var userId = GetUserId();
            await _teamService.DeleteTeamAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Retrieves all members of a specific team.
        /// </summary>
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetTeamMembers(int id)
        {
            var members = await _teamService.GetTeamMembersAsync(id);
            return Ok(ApiResponse<IEnumerable<TeamMemberDto>>.Ok(members, "Team members retrieved successfully"));
        }

        /// <summary>
        /// Adds a new member to a specific team.
        /// </summary>
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddTeamMember(int id, [FromBody] AddTeamMemberRequest request)
        {
            await _teamService.AddTeamMemberAsync(id, request);
            return StatusCode(201, ApiResponse<string>.Ok("Member added", "Team member added successfully"));
        }

        /// <summary>
        /// Removes a member from a specific team.
        /// </summary>
        [HttpDelete("{id}/members/{memberUserId}")]
        public async Task<IActionResult> RemoveTeamMember(int id, int memberUserId)
        {
            var userId = GetUserId();
            await _teamService.RemoveTeamMemberAsync(userId, id, memberUserId);
            return NoContent();
        }

        /// <summary>
        /// Update a team member's role.
        /// </summary>
        [HttpPut("{id}/members/{memberUserId}/role")]
        public async Task<IActionResult> UpdateTeamMemberRole(int id, int memberUserId, [FromBody] UpdateTeamMemberRoleRequest request)
        {
            var userId = GetUserId();
            await _teamService.UpdateTeamMemberRoleAsync(userId, id, memberUserId, (Maui_Task.Shared.Data.Entities.TeamRole)request.Role);
            return Ok(ApiResponse<string>.Ok("Role updated", "Member role updated successfully"));
        }
    }
}
