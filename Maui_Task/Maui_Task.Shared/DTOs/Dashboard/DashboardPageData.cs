using System.Collections.Generic;
using Maui_Task.Shared.DTOs.Tasks;

namespace Maui_Task.Shared.DTOs.Dashboard
{
    public class DashboardPageData
    {
        public DashboardStatsDto? Stats { get; set; }
        public List<TaskDto> ActiveTasks { get; set; } = new();
        public List<ActivityItemDto> RecentActivity { get; set; } = new();
        public string? UserName { get; set; }
    }
}