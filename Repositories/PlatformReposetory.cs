
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;
using Microsoft.EntityFrameworkCore;
namespace Repositories
{
    public class PlatformRepository : IPlatformRepository
    {
        MyShopContext _context;
        public PlatformRepository(MyShopContext context)
        {
            _context = context;
        }
        public async Task<Platform?> GetPlatformByNameAsync(string platformName)
        {
            return await _context.Platforms.FirstOrDefaultAsync(p => p.PlatformName == platformName);
        }

        async public Task<Platform?> GetPlatformByIdAsync(int id)
        {
            return await _context.Platforms.FirstOrDefaultAsync(p => p.PlatformId == id);
        }

        async public Task<IEnumerable<Platform>> GetPlatformsAsync()
        {
            return await _context.Platforms.ToListAsync();
        }

        async public Task<Platform> AddPlatformAsync(Platform platform)
        {
            await _context.Platforms.AddAsync(platform);
            await _context.SaveChangesAsync();
            return platform;
        }

        async public Task<bool> UpdatePlatformAsync(int id, Platform platform)
        {
            var existing = await _context.Platforms.FirstOrDefaultAsync(p => p.PlatformId == id);
            if (existing == null)
            {
                return false;
            }

            var conflict = await _context.Platforms.FirstOrDefaultAsync(p => p.PlatformName == platform.PlatformName && p.PlatformId != id);
            if (conflict != null)
            {
                return false;
            }

            _context.Entry(existing).CurrentValues.SetValues(platform);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ReassignPlatformReferencesAsync(int platformId, int defaultPlatformId)
        {
            var cartItems = await _context.CartItems.Where(ci => ci.PlatformId == platformId).ToListAsync();
            foreach (var ci in cartItems)
                ci.PlatformId = defaultPlatformId;

            var orderItems = await _context.OrderItems.Where(oi => oi.PlatformId == platformId).ToListAsync();
            foreach (var oi in orderItems)
                oi.PlatformId = defaultPlatformId;

            var basicSites = await _context.BasicSites.Where(bs => bs.PlatformId == platformId).ToListAsync();
            foreach (var bs in basicSites)
                bs.PlatformId = defaultPlatformId;

            await _context.SaveChangesAsync();
        }

        async public Task<bool> DeletePlatformAsync(int id)
        {
            var platformObjectToDelete = await _context.Platforms.FirstOrDefaultAsync(x => x.PlatformId == id);
            if (platformObjectToDelete == null)
            {
                return false;
            }

            _context.Platforms.Remove(platformObjectToDelete);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
