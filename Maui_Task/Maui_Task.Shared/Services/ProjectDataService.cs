using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Projects;
using Maui_Task.Shared.Helpers;

namespace Maui_Task.Shared.Services
{
    public class ProjectDataService : IProjectDataService
    {
        private readonly HttpApiService _api;
        private readonly AppDbContext _db;
        private readonly ISyncQueueService _syncQueue;

        public ProjectDataService(HttpApiService api, AppDbContext db, ISyncQueueService syncQueue)
        {
            _api = api;
            _db = db;
            _syncQueue = syncQueue;
        }

        public async Task<List<ProjectDto>> GetProjectsAsync()
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<IEnumerable<ProjectDto>>>("/api/projects");
                return response?.Data?.ToList() ?? new List<ProjectDto>();
            }
            catch (HttpRequestException)
            {
                return await LoadFromLocalAsync();
            }
            catch
            {
                return await LoadFromLocalAsync();
            }
        }

        public async Task<ProjectDto?> GetProjectAsync(int id)
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<ProjectDto>>($"/api/projects/{id}");
                return response?.Data;
            }
            catch (HttpRequestException)
            {
                var local = await LoadLocalProjectAsync(id);
                return local is null ? null : Map(local);
            }
            catch
            {
                var local = await LoadLocalProjectAsync(id);
                return local is null ? null : Map(local);
            }
        }

        public async Task<ProjectDto?> SaveProjectAsync(CreateProjectRequest request, int? projectId = null)
        {
            try
            {
                ApiResponse<ProjectDto>? response;
                if (projectId.HasValue && projectId.Value > 0)
                {
                    response = await _api.PutAsync<ApiResponse<ProjectDto>>($"/api/projects/{projectId.Value}", new UpdateProjectRequest
                    {
                        Name = request.Name,
                        Description = request.Description,
                        Color = request.Color
                    });
                }
                else
                {
                    response = await _api.PostAsync<ApiResponse<ProjectDto>>("/api/projects", request);
                }

                if (response?.Data != null)
                {
                    return response.Data;
                }
            }
            catch (HttpRequestException)
            {
                // fall back to local database
            }
            catch
            {
                // fall back to local database
            }

            var saved = await SaveLocalAsync(request, projectId);
            if (saved is not null)
            {
                await EnqueueProjectSaveAsync(request, saved.Id, projectId.HasValue && projectId.Value > 0 ? "update" : "create");
            }

            return saved;
        }

        public async Task DeleteProjectAsync(int id)
        {
            try
            {
                await _api.DeleteAsync($"/api/projects/{id}");
                return;
            }
            catch (HttpRequestException)
            {
                // fall back to local database
            }
            catch
            {
                // fall back to local database
            }

            var local = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (local is null)
            {
                return;
            }

            _db.Projects.Remove(local);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Project", "delete", new ProjectIdSyncPayload(id));
        }

        private async Task<List<ProjectDto>> LoadFromLocalAsync()
        {
            var localProjects = await _db.Projects
                .AsNoTracking()
                .Include(p => p.Owner)
                .Include(p => p.Members)
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return localProjects.Select(Map).ToList();
        }

        private async Task<Project?> LoadLocalProjectAsync(int id)
        {
            return await _db.Projects
                .AsNoTracking()
                .Include(p => p.Owner)
                .Include(p => p.Members)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        private async Task<ProjectDto?> SaveLocalAsync(CreateProjectRequest request, int? projectId)
        {
            Project entity;
            if (projectId.HasValue && projectId.Value > 0)
            {
                entity = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId.Value) ?? new Project();
                if (entity.Id == 0)
                {
                    _db.Projects.Add(entity);
                }
            }
            else
            {
                entity = new Project();
                _db.Projects.Add(entity);
            }

            var ownerId = await ResolveOwnerIdAsync();

            entity.Name = request.Name;
            entity.Description = request.Description;
            entity.Color = string.IsNullOrWhiteSpace(request.Color) ? "#3B82F6" : request.Color;
            entity.OwnerId = ownerId;
            entity.IsStarred = entity.IsStarred;
            entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;

            await _db.SaveChangesAsync();
            await _db.Entry(entity).Reference(p => p.Owner).LoadAsync();
            return Map(entity);
        }

        private async Task<int> ResolveOwnerIdAsync()
        {
            var currentUserId = await _db.AppUsers.Select(u => u.Id).FirstOrDefaultAsync();
            if (currentUserId > 0)
            {
                return currentUserId;
            }

            var fallbackUser = new AppUser
            {
                FullName = "Offline User",
                FirstName = "Offline",
                LastName = "User",
                Email = $"offline-{Guid.NewGuid():N}@local.taskflow",
                PasswordHash = "offline",
                CreatedAt = DateTime.UtcNow
            };

            _db.AppUsers.Add(fallbackUser);
            await _db.SaveChangesAsync();
            return fallbackUser.Id;
        }

        private static ProjectDto Map(Project project)
        {
            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Color = project.Color,
                OwnerId = project.OwnerId,
                OwnerName = string.IsNullOrWhiteSpace(project.Owner?.FullName) ? "Unknown Owner" : project.Owner.FullName,
                IsStarred = project.IsStarred,
                CreatedAt = project.CreatedAt,
                TasksTotal = project.Tasks.Count,
                TasksCompleted = project.Tasks.Count(t => t.Status == Maui_Task.Shared.Data.Entities.TaskStatus.Completed),
                MemberCount = project.Members.Count
            };
        }

        private async Task EnqueueProjectSaveAsync(CreateProjectRequest request, int projectId, string operation)
        {
            await _syncQueue.EnqueueAsync(
                "Project",
                operation,
                new ProjectSyncPayload(projectId, request));
        }
    }
}
