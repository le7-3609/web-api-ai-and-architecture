using Entities;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public class StatusRepository : IStatusRepository
    {
        MyShopContext _context;

        public StatusRepository(MyShopContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Status>> GetAllStatusesAsync()
        {
            return await _context.Statuses.ToListAsync();
        }

        public async Task<Status?> GetStatusByIdAsync(int id)
        {
            return await _context.Statuses.FirstOrDefaultAsync(s => s.StatusId == id);
        }
    }
}
