using ABCRetails.Models;
using Microsoft.AspNetCore.Identity;
using PROG6212POE.Models;

namespace ABCRetails.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetByIdAsync(string id);
        Task<IdentityResult> CreateUserAsync(User user, string password);
        Task<IdentityResult> AddRoleAsync(User user, string role);
    }
}
