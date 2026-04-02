using System.Collections.Generic;
using System.Threading.Tasks;
using Maui_Task.Shared.Data.Entities;

namespace Maui_Task.Shared.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<AppUser?> GetByIdAsync(int id);
        Task<AppUser?> GetByEmailAsync(string email);
        Task<AppUser?> GetByUsernameAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<AppUser> CreateAsync(AppUser user);
        Task<AppUser> UpdateAsync(AppUser user);
        Task DeleteAsync(int id);
        Task<List<AppUser>> GetAllAsync();
    }
}
