using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maui_Task.Shared.Data;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maui_Task.Shared.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public UserRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<AppUser?> GetByIdAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<AppUser?> GetByEmailAsync(string email)
        {
            var normalized = email.Trim().ToLowerInvariant();
            await using var db = await _factory.CreateDbContextAsync();
            return await db.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
        }

        public async Task<AppUser?> GetByUsernameAsync(string username)
        {
            var normalized = username.Trim().ToLowerInvariant();
            await using var db = await _factory.CreateDbContextAsync();
            return await db.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var normalized = email.Trim().ToLowerInvariant();
            await using var db = await _factory.CreateDbContextAsync();
            return await db.AppUsers.AnyAsync(u => u.Email.ToLower() == normalized);
        }

        public async Task<AppUser> CreateAsync(AppUser user)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.AppUsers.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<AppUser> UpdateAsync(AppUser user)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.AppUsers.Update(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return;
            }

            db.AppUsers.Remove(user);
            await db.SaveChangesAsync();
        }

        public async Task<List<AppUser>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.AppUsers
                .AsNoTracking()
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }
    }
}
