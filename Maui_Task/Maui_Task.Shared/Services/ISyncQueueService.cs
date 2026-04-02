using System.Threading.Tasks;

namespace Maui_Task.Shared.Services
{
    public interface ISyncQueueService
    {
        Task EnqueueAsync(string entityName, string operation, object payload);
        Task ProcessPendingAsync();
    }
}
