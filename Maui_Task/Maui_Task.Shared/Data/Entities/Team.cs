using System;
using System.Collections.Generic;

namespace Maui_Task.Shared.Data.Entities
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser Owner { get; set; } = null!;
        public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    }
}
