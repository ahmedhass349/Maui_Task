using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Shared.DTOs.Dashboard;
using Maui_Task.Shared.DTOs.Tasks;
using Maui_Task.Shared.Helpers;

namespace Maui_Task.Shared.Services
{
    public class DashboardDataService : IDashboardDataService
    {
        private readonly HttpApiService _api;
        private readonly ITaskDataService _tasks;
        private readonly TaskFlowAuthStateProvider _authState;

        public DashboardDataService(HttpApiService api, ITaskDataService tasks, TaskFlowAuthStateProvider authState)
        {
            _api = api;
            _tasks = tasks;
            _authState = authState;
        }

        public async Task<DashboardPageData> LoadDashboardAsync()
        {
            var data = new DashboardPageData();

            try
            {
                var statsTask = SafeGetAsync<ApiResponse<DashboardStatsDto>>("api/dashboard/stats");
                var tasksTask = SafeGetAsync<ApiResponse<List<TaskDto>>>("api/tasks");
                var activityTask = SafeGetAsync<ApiResponse<List<ActivityItemDto>>>("api/dashboard/activity");
                var userTask = SafeGetAsync<ApiResponse<UserDto>>("api/auth/me");

                await Task.WhenAll(statsTask, tasksTask, activityTask, userTask);

                var statsResponse = await statsTask;
                if (statsResponse?.Success == true)
                {
                    data.Stats = statsResponse.Data;
                }

                var tasksResponse = await tasksTask;
                if (tasksResponse?.Success == true && tasksResponse.Data != null)
                {
                    data.ActiveTasks = tasksResponse.Data
                        .Where(t => !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    data.ActiveTasks = (await _tasks.GetTasksAsync())
                        .Where(t => !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var activityResponse = await activityTask;
                if (activityResponse?.Success == true && activityResponse.Data != null)
                {
                    data.RecentActivity = activityResponse.Data;
                }

                var userResponse = await userTask;
                if (userResponse?.Success == true)
                {
                    data.UserName = userResponse.Data?.FullName;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(data.UserName))
            {
                data.UserName = _authState.CurrentUser?.FullName;
            }

            return data;
        }

        private async Task<T?> SafeGetAsync<T>(string uri)
        {
            try
            {
                return await _api.GetAsync<T>(uri);
            }
            catch
            {
                return default;
            }
        }
    }
}