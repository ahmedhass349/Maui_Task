using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ProjectRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<Project>> GetAllByUserIdAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Projects
                .AsNoTracking()
                .Include(p => p.Owner)
                .Include(p => p.Members)
                .Include(p => p.Tasks)
                .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Projects
                .AsNoTracking()
                .Include(p => p.Owner)
                .Include(p => p.Members)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project> CreateAsync(Project project)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return project;
        }

        public async Task<Project> UpdateAsync(Project project)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.Projects.Update(project);
            await db.SaveChangesAsync();
            return project;
        }

        public async Task DeleteAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return;
            }

            db.Projects.Remove(project);
            await db.SaveChangesAsync();
        }
    }
}
