using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Tasks;
using Maui_Task.Shared.Helpers;

namespace Maui_Task.Shared.Services
{
    public class TaskDataService : ITaskDataService
    {
        private readonly HttpApiService _api;
        private readonly AppDbContext _db;
        private readonly ISyncQueueService _syncQueue;

        public TaskDataService(HttpApiService api, AppDbContext db, ISyncQueueService syncQueue)
        {
            _api = api;
            _db = db;
            _syncQueue = syncQueue;
        }

        public async Task<List<TaskDto>> GetTasksAsync()
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<IEnumerable<TaskDto>>>("/api/tasks");
                return response?.Data?.ToList() ?? new List<TaskDto>();
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

        public async Task<TaskDto?> GetTaskAsync(int id)
        {
            try
            {
                var response = await _api.GetAsync<ApiResponse<TaskDto>>($"/api/tasks/{id}");
                return response?.Data;
            }
            catch (HttpRequestException)
            {
                var local = await _db.TaskItems
                    .AsNoTracking()
                    .Include(t => t.Project)
                    .Include(t => t.Assignee)
                    .FirstOrDefaultAsync(t => t.Id == id);

                return local is null ? null : Map(local);
            }
            catch
            {
                var local = await _db.TaskItems
                    .AsNoTracking()
                    .Include(t => t.Project)
                    .Include(t => t.Assignee)
                    .FirstOrDefaultAsync(t => t.Id == id);

                return local is null ? null : Map(local);
            }
        }

        public async Task<TaskDto?> SaveTaskAsync(CreateTaskRequest request, int? taskId = null)
        {
            try
            {
                ApiResponse<TaskDto>? response;
                if (taskId.HasValue && taskId.Value > 0)
                {
                    response = await _api.PutAsync<ApiResponse<TaskDto>>($"/api/tasks/{taskId.Value}", request);
                }
                else
                {
                    response = await _api.PostAsync<ApiResponse<TaskDto>>("/api/tasks", request);
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

            var saved = await SaveLocalAsync(request, taskId);
            if (saved is not null)
            {
                await EnqueueTaskSaveAsync(request, saved.Id, taskId.HasValue && taskId.Value > 0 ? "update" : "create");
            }

            return saved;
        }

        public async Task<TaskDto?> ToggleStarAsync(int taskId)
        {
            try
            {
                var response = await _api.PatchAsync<ApiResponse<TaskDto>>($"/api/tasks/{taskId}/star", null);
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

            var local = await _db.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (local is null)
            {
                return null;
            }

            local.IsStarred = !local.IsStarred;
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Task", "star", new TaskIdSyncPayload(taskId));
            return Map(local);
        }

        public async Task<TaskDto?> UpdateStatusAsync(int taskId, string status)
        {
            try
            {
                var response = await _api.PatchAsync<ApiResponse<TaskDto>>($"/api/tasks/{taskId}/status", new UpdateStatusRequest { Status = status });
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

            var local = await _db.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (local is null)
            {
                return null;
            }

            if (Enum.TryParse<Maui_Task.Shared.Data.Entities.TaskStatus>(status, true, out var parsedStatus))
            {
                local.Status = parsedStatus;
                await _db.SaveChangesAsync();
                await _syncQueue.EnqueueAsync("Task", "status", new TaskStatusSyncPayload(taskId, status));
            }

            return Map(local);
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            try
            {
                await _api.DeleteAsync($"/api/tasks/{taskId}");
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

            var local = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);
            if (local is null)
            {
                return;
            }

            _db.TaskItems.Remove(local);
            await _db.SaveChangesAsync();
            await _syncQueue.EnqueueAsync("Task", "delete", new TaskIdSyncPayload(taskId));
        }

        private async Task<List<TaskDto>> LoadFromLocalAsync()
        {
            var localTasks = await _db.TaskItems
                .AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return localTasks.Select(Map).ToList();
        }

        private async Task<TaskDto?> SaveLocalAsync(CreateTaskRequest request, int? taskId)
        {
            TaskItem entity;
            if (taskId.HasValue && taskId.Value > 0)
            {
                entity = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId.Value) ?? new TaskItem();
                if (entity.Id == 0)
                {
                    _db.TaskItems.Add(entity);
                }
            }
            else
            {
                entity = new TaskItem();
                _db.TaskItems.Add(entity);
            }

            entity.Title = request.Title;
            entity.Description = request.Description;
            entity.ProjectId = request.ProjectId;
            entity.AssigneeId = request.AssigneeId;
            entity.Priority = request.Priority;
            entity.Status = request.Status;
            entity.DueDate = request.DueDate;
            entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;

            await _db.SaveChangesAsync();

            await _db.Entry(entity).Reference(t => t.Project).LoadAsync();
            await _db.Entry(entity).Reference(t => t.Assignee).LoadAsync();

            return Map(entity);
        }

        private static TaskDto Map(TaskItem task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                ProjectName = task.Project?.Name ?? "No Project",
                AssigneeName = task.Assignee?.FullName,
                DueDate = task.DueDate,
                DueDateLabel = task.DueDate?.ToString("MMM dd"),
                Priority = task.Priority.ToString(),
                Status = task.Status.ToString(),
                IsStarred = task.IsStarred,
                CreatedAt = task.CreatedAt
            };
        }

        private async Task EnqueueTaskSaveAsync(CreateTaskRequest request, int taskId, string operation)
        {
            await _syncQueue.EnqueueAsync(
                "Task",
                operation,
                new TaskSyncPayload(taskId, request));
        }
    }
}
