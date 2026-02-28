using Entities;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public class SiteTypeRepository : ISiteTypeRepository
    {
        MyShopContext _context;

        public SiteTypeRepository(MyShopContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<SiteType>?> GetAllAsync()
        {
            return await _context.SiteTypes.ToListAsync();
        }
        public async Task<SiteType?> GetByIdAsync(int id)
        {
            return await _context.SiteTypes.FirstOrDefaultAsync(u => u.SiteTypeId == id);
        }
        public async Task<SiteType?> GetByNameAsync(string name)
        {
            return await _context.SiteTypes.FirstOrDefaultAsync(u => u.SiteTypeName == name);
        }
        public async Task<SiteType> UpdateByMngAsync( SiteType siteType)
        {
            _context.SiteTypes.Update(siteType);
            await _context.SaveChangesAsync();
            return siteType;
        }

    }
}