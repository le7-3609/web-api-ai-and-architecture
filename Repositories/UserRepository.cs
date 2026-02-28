using Entities;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MyShopContext _context;

        public UserRepository(MyShopContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<User>?> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }
        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetByEmailAsync(string email,int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.UserId != id);
        }

        public async Task<User> RegisterAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
        }

        public async Task<User?> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<IEnumerable<Order>?> GetAllOrdersAsync(int userId)
        {
            return await _context.Orders
                .Where(o=>o.UserId == userId)
                .Include(o => o.StatusNavigation)
                .Include(o => o.BasicSite)
                    .ThenInclude(bs => bs.SiteType)
                .Include(o => o.Reviews)
                .ToListAsync();
        }

        public async Task<User?> GetByProviderIdAsync(string provider, string providerId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderId == providerId);
        }
    }
}