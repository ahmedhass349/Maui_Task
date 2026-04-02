using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Teams;
using Maui_Task.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Services
{
    public class TeamDataService : ITeamDataService
    {
        private readonly HttpApiService _api;
        private readonly AppDbContext _db;
        private readonly TaskFlowAuthStateProvider _authState;
        private readonly ISyncQueueService _syncQueue;

        public TeamDataService(HttpApiService api, AppDbContext db, TaskFlowAuthStateProvider authState, ISyncQueueService syncQueue)
        {
            _api = api;
            _db = db;
            _authState = authState;
            _syncQueue = syncQueue;
        }

        public async Task<List<TeamMemberCardDto>> GetTeamMembersAsync()
        {
            try
            {
                var teamsResponse = await _api.GetAsync<ApiResponse<List<TeamDto>>>("/api/teams");
                if (teamsResponse?.Success == true && teamsResponse.Data != null)
                {
                    var members = new List<TeamMemberCardDto>();
                    foreach (var team in teamsResponse.Data)
                    {
                        var membersResponse = await _api.GetAsync<ApiResponse<IEnumerable<TeamMemberDto>>>($"/api/teams/{team.Id}/members");
                        var teamMembers = membersResponse?.Data?.ToList() ?? new List<TeamMemberDto>();
                        if (teamMembers.Count == 0)
                        {
                            members.Add(new TeamMemberCardDto
                            {
                                Id = team.Id,
                                Name = team.Name,
                                Role = $"Owner: {team.OwnerName}",
                                Email = $"{team.MemberCount} members",
                                Status = "online",
                                TasksCompleted = 0,
                                TasksInProgress = 0,
                                Initials = BuildInitials(team.Name)
                            });
                        }
                        else
                        {
                            members.AddRange(teamMembers.Select(member => new TeamMemberCardDto
                            {
                                Id = member.UserId,
                                Name = member.UserName,
                                Role = member.Role,
                                Email = member.Email,
                                Status = "online",
                                TasksCompleted = member.TasksCompleted,
                                TasksInProgress = member.TasksInProgress,
                                Initials = string.IsNullOrWhiteSpace(member.Initials) ? BuildInitials(member.UserName) : member.Initials
                            }));
                        }
                    }

                    return members;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            return await LoadLocalTeamMembersAsync();
        }

        public async Task<List<TeamDto>> GetTeamsAsync()
        {
            try
            {
                var teams = await _api.GetAsync<List<TeamDto>>("/api/teams");
                if (teams != null)
                {
                    return teams;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var localTeams = await _db.Teams
                .AsNoTracking()
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return localTeams.Select(Map).ToList();
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            try
            {
                var users = await _api.GetAsync<List<UserDto>>("api/users");
                if (users != null)
                {
                    return users;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            return await _db.AppUsers
                .AsNoTracking()
                .OrderBy(u => u.FirstName)
                .Select(user => new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Company = user.Company,
                    Country = user.Country,
                    Phone = user.Phone,
                    Timezone = user.Timezone,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                })
                .ToListAsync();
        }

        public async Task<TeamDto?> GetTeamAsync(int id)
        {
            try
            {
                var team = await _api.GetAsync<TeamDto>($"/api/teams/{id}");
                if (team != null)
                {
                    return team;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var local = await _db.Teams.AsNoTracking().Include(t => t.Owner).FirstOrDefaultAsync(t => t.Id == id);
            return local is null ? null : Map(local);
        }

        public async Task<TeamDto?> SaveTeamAsync(int id, TeamDto team)
        {
            try
            {
                if (id == 0)
                {
                    var created = await _api.PostAsync<TeamDto>("/api/teams", team);
                    if (created != null)
                    {
                        return created;
                    }
                }
                else
                {
                    var updated = await _api.PutAsync<TeamDto>($"/api/teams/{id}", team);
                    if (updated != null)
                    {
                        return updated;
                    }
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            var saved = await SaveLocalAsync(id, team);
            if (saved is not null)
            {
                await _syncQueue.EnqueueAsync("Team", id == 0 ? "create" : "update", new TeamSyncPayload(saved.Id, team));
            }

            return saved;
        }

        private async Task<List<TeamMemberCardDto>> LoadLocalTeamMembersAsync()
        {
            var teams = await _db.Teams
                .AsNoTracking()
                .Include(t => t.Owner)
                .Include(t => t.Members)
                    .ThenInclude(tm => tm.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var results = new List<TeamMemberCardDto>();
            foreach (var team in teams)
            {
                if (team.Members.Count == 0)
                {
                    results.Add(new TeamMemberCardDto
                    {
                        Id = team.Id,
                        Name = team.Name,
                        Role = $"Owner: {team.Owner?.FullName ?? "Unknown Owner"}",
                        Email = "0 members",
                        Status = ResolveStatus(team.Owner),
                        TasksCompleted = await CountCompletedTasksAsync(team.OwnerId),
                        TasksInProgress = await CountInProgressTasksAsync(team.OwnerId),
                        Initials = BuildInitials(team.Name),
                        LastActiveAt = team.Owner?.LastLoginAt
                    });
                }
                else
                {
                    foreach (var member in team.Members)
                    {
                        results.Add(new TeamMemberCardDto
                        {
                            Id = member.UserId,
                            Name = member.User?.FullName ?? string.Empty,
                            Role = member.Role.ToString(),
                            Email = member.User?.Email ?? string.Empty,
                            Status = ResolveStatus(member.User),
                            TasksCompleted = await CountCompletedTasksAsync(member.UserId),
                            TasksInProgress = await CountInProgressTasksAsync(member.UserId),
                            Initials = BuildInitials(member.User?.FullName ?? string.Empty),
                            LastActiveAt = member.User?.LastLoginAt
                        });
                    }
                }
            }

            return results;
        }

        private async Task<TeamDto?> SaveLocalAsync(int id, TeamDto team)
        {
            Team entity;
            if (id > 0)
            {
                entity = await _db.Teams.Include(t => t.Owner).FirstOrDefaultAsync(t => t.Id == id) ?? new Team();
                if (entity.Id == 0)
                {
                    _db.Teams.Add(entity);
                }
            }
            else
            {
                entity = new Team();
                _db.Teams.Add(entity);
            }

            entity.Name = team.Name.Trim();
            entity.Description = team.Description;
            entity.OwnerId = team.OwnerId > 0 ? team.OwnerId : await ResolveOwnerIdAsync();
            entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;

            await _db.SaveChangesAsync();
            await _db.Entry(entity).Reference(t => t.Owner).LoadAsync();
            return Map(entity);
        }

        private async Task<int> ResolveOwnerIdAsync()
        {
            var currentUserId = _authState.CurrentUser?.Id ?? 0;
            if (currentUserId > 0)
            {
                return currentUserId;
            }

            var user = await _db.AppUsers.OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt).FirstOrDefaultAsync();
            if (user != null)
            {
                return user.Id;
            }

            user = new AppUser
            {
                FirstName = "Offline",
                LastName = "Owner",
                FullName = "Offline Owner",
                Email = $"offline-{Guid.NewGuid():N}@local.taskflow",
                PasswordHash = string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();
            return user.Id;
        }

        private async Task<int> CountCompletedTasksAsync(int userId)
        {
            return await _db.TaskItems.CountAsync(t => t.AssigneeId == userId && t.Status == Maui_Task.Shared.Data.Entities.TaskStatus.Completed);
        }

        private async Task<int> CountInProgressTasksAsync(int userId)
        {
            return await _db.TaskItems.CountAsync(t => t.AssigneeId == userId && t.Status == Maui_Task.Shared.Data.Entities.TaskStatus.InProgress);
        }

        private static TeamDto Map(Team team)
        {
            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                OwnerId = team.OwnerId,
                OwnerName = team.Owner?.FullName ?? string.Empty,
                CreatedAt = team.CreatedAt,
                MemberCount = team.Members?.Count ?? 0
            };
        }

        private static string ResolveStatus(AppUser? user)
        {
            if (user?.LastLoginAt is null)
            {
                return "offline";
            }

            return user.LastLoginAt.Value >= DateTime.UtcNow.AddHours(-8) ? "online" : "away";
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "U";
            }

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => char.ToUpperInvariant(part[0]));
            var initials = new string(parts.ToArray());
            return string.IsNullOrWhiteSpace(initials) ? "U" : initials;
        }
    }
}