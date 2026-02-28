using Entities;

namespace Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>?> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email, int id);
        Task<User> RegisterAsync(User user);
        Task<User?> LoginAsync(string email, string password);
        Task<User?> UpdateAsync(User user);
        Task<IEnumerable<Order>?> GetAllOrdersAsync(int userId);
        Task<User?> GetByProviderIdAsync(string provider, string providerId);
    }
}