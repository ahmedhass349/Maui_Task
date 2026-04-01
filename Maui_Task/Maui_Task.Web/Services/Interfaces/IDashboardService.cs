using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Dashboard;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync(int userId);
        Task<IEnumerable<ActivityItemDto>> GetRecentActivityAsync(int userId);
    }
}
