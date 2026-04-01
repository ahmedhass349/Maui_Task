// FILE: Services/TeamService.cs
// STATUS: UPDATED
// CHANGES: Added UpdateTeam/DeleteTeam/RemoveTeamMember (#22),
//          Fixed N+1 in GetTeamMembersAsync (#12),
//          Fixed double SaveChangesAsync in CreateTeamAsync (#14),
//          Added Initials to TeamMemberDto (#30)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Teams;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;
using TaskStatus = Maui_Task.Shared.Data.Entities.TaskStatus;

namespace Maui_Task.Web.Services
{
    public class TeamService : ITeamService
    {
        private readonly IGenericRepository<Team> _teamRepository;
        private readonly IGenericRepository<TeamMember> _teamMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public TeamService(
            IGenericRepository<Team> teamRepository,
            IGenericRepository<TeamMember> teamMemberRepository,
            IUserRepository userRepository,
            ITaskRepository taskRepository,
            IMapper mapper)
        {
            _teamRepository = teamRepository;
            _teamMemberRepository = teamMemberRepository;
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TeamDto>> GetUserTeamsAsync(int userId)
        {
            var teams = await _teamRepository.Query()
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .Where(t => t.OwnerId == userId || t.Members.Any(m => m.UserId == userId))
                .ToListAsync();

            return _mapper.Map<IEnumerable<TeamDto>>(teams);
        }

        public async Task<TeamDto> CreateTeamAsync(int userId, CreateTeamRequest request)
        {
            var team = new Team
            {
                Name = request.Name,
                Description = request.Description,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _teamRepository.AddAsync(team);
            await _teamRepository.SaveChangesAsync();

            // Fix #14: Use single SaveChangesAsync instead of two
            var ownerMember = new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                Role = Maui_Task.Shared.Data.Entities.TeamRole.Admin
            };

            await _teamMemberRepository.AddAsync(ownerMember);
            await _teamMemberRepository.SaveChangesAsync();

            var savedTeam = await _teamRepository.Query()
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == team.Id);

            return _mapper.Map<TeamDto>(savedTeam!);
        }

        public async Task<TeamDto> UpdateTeamAsync(int userId, int teamId, UpdateTeamRequest request)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            // Only owner can update
            if (team.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to update this team.");

            team.Name = request.Name;
            if (request.Description != null)
                team.Description = request.Description;

            _teamRepository.Update(team);
            await _teamRepository.SaveChangesAsync();

            var saved = await _teamRepository.Query()
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == team.Id);

            return _mapper.Map<TeamDto>(saved!);
        }

        public async Task DeleteTeamAsync(int userId, int teamId)
        {
            var team = await _teamRepository.Query()
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            // Only owner can delete
            if (team.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this team.");

            // Remove all members first
            foreach (var member in team.Members.ToList())
            {
                _teamMemberRepository.Remove(member);
            }

            _teamRepository.Remove(team);
            await _teamRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int teamId)
        {
            var team = await _teamRepository.Query()
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            // Fix #12: Batch-load task counts instead of N+1
            var memberUserIds = team.Members.Select(m => m.UserId).ToList();

            var completedCounts = await _taskRepository.Query()
                .Where(t => t.AssigneeId.HasValue && memberUserIds.Contains(t.AssigneeId.Value) && t.Status == TaskStatus.Completed)
                .GroupBy(t => t.AssigneeId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var inProgressCounts = await _taskRepository.Query()
                .Where(t => t.AssigneeId.HasValue && memberUserIds.Contains(t.AssigneeId.Value) && t.Status == TaskStatus.InProgress)
                .GroupBy(t => t.AssigneeId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var result = new List<TeamMemberDto>();
            foreach (var member in team.Members)
            {
                // Compute initials (#30)
                var nameParts = (member.User?.FullName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string initials = nameParts.Length >= 2
                    ? $"{nameParts[0][0]}{nameParts[^1][0]}".ToUpperInvariant()
                    : nameParts.Length == 1 ? nameParts[0][0].ToString().ToUpperInvariant() : "?";

                completedCounts.TryGetValue(member.UserId, out int completedCount);
                inProgressCounts.TryGetValue(member.UserId, out int inProgressCount);

                result.Add(new TeamMemberDto
                {
                    UserId = member.UserId,
                    UserName = member.User?.FullName ?? string.Empty,
                    Email = member.User?.Email ?? string.Empty,
                    AvatarUrl = member.User?.AvatarUrl,
                    Initials = initials,
                    Role = member.Role.ToString(),
                    TasksCompleted = completedCount,
                    TasksInProgress = inProgressCount
                });
            }

            return result;
        }

        public async Task AddTeamMemberAsync(int teamId, AddTeamMemberRequest request)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new KeyNotFoundException($"User with email '{request.Email}' not found.");

            bool alreadyMember = await _teamMemberRepository.ExistsAsync(
                tm => tm.TeamId == teamId && tm.UserId == user.Id);

            if (alreadyMember)
                throw new InvalidOperationException("User is already a member of this team.");

            var teamMember = new TeamMember
            {
                TeamId = teamId,
                UserId = user.Id,
                Role = (Maui_Task.Shared.Data.Entities.TeamRole)request.Role
            };

            await _teamMemberRepository.AddAsync(teamMember);
            await _teamMemberRepository.SaveChangesAsync();
        }

        public async Task RemoveTeamMemberAsync(int userId, int teamId, int memberUserId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            // Only owner can remove members
            if (team.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to remove members from this team.");

            var membership = await _teamMemberRepository.FirstOrDefaultAsync(
                tm => tm.TeamId == teamId && tm.UserId == memberUserId);

            if (membership == null)
                throw new KeyNotFoundException($"User with ID {memberUserId} is not a member of this team.");

            // Cannot remove the owner
            if (memberUserId == team.OwnerId)
                throw new InvalidOperationException("Cannot remove the team owner.");

            _teamMemberRepository.Remove(membership);
            await _teamMemberRepository.SaveChangesAsync();
        }

        public async Task UpdateTeamMemberRoleAsync(int userId, int teamId, int memberUserId, Maui_Task.Shared.Data.Entities.TeamRole role)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");

            // Only owner can change member roles
            if (team.OwnerId != userId)
                throw new UnauthorizedAccessException("You do not have permission to change member roles for this team.");

            var membership = await _teamMemberRepository.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == memberUserId);
            if (membership == null)
                throw new KeyNotFoundException($"User with ID {memberUserId} is not a member of this team.");

            // Prevent changing owner role
            if (memberUserId == team.OwnerId)
                throw new InvalidOperationException("Cannot change the owner's role.");

            membership.Role = role;
            _teamMemberRepository.Update(membership);
            await _teamMemberRepository.SaveChangesAsync();
        }
    }
}
