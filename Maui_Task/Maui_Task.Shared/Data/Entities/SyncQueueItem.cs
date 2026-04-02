using System;

namespace Maui_Task.Shared.Data.Entities
{
    public class SyncQueueItem
    {
        public int Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAttemptAt { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }
    }
}
