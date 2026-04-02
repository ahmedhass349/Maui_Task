using System.Threading.Tasks;
using Maui_Task.Shared.DTOs.Dashboard;

namespace Maui_Task.Shared.Services
{
    public interface IDashboardDataService
    {
        Task<DashboardPageData> LoadDashboardAsync();
    }
}