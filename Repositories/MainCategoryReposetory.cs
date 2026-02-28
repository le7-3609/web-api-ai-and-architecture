using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;
using Microsoft.EntityFrameworkCore;
namespace Repositories
{
    public class MainCategoryRepository : IMainCategoryRepository
    {
        MyShopContext _context;
        public MainCategoryRepository(MyShopContext context)
        {
            _context = context;
        }

        async public Task<IEnumerable<MainCategory>> GetMainCategoriesAsync()
        {
            return await _context.MainCategories.ToListAsync();
        }

        async public Task<MainCategory?> GetMainCategoryByIdAsync(int id)
        {
            return await _context.MainCategories.FirstOrDefaultAsync(x => x.MainCategoryId == id);
        }

        async public Task<MainCategory> AddMainCategoryAsync(MainCategory mainCategoryToAdd)
        {

            await _context.MainCategories.AddAsync(mainCategoryToAdd);
            await _context.SaveChangesAsync();
            return mainCategoryToAdd;
        }

        async public Task UpdateMainCategoryAsync(MainCategory mainCategoryToUpdate)
        {

            // If an instance with the same key is already tracked, apply the new values
            var local = _context.MainCategories.Local.FirstOrDefault(e => e.MainCategoryId == mainCategoryToUpdate.MainCategoryId);
            if (local != null && !ReferenceEquals(local, mainCategoryToUpdate))
            {
                _context.Entry(local).CurrentValues.SetValues(mainCategoryToUpdate);
            }
            else
            {
                _context.MainCategories.Update(mainCategoryToUpdate);
            }

            await _context.SaveChangesAsync();

        }

        async public Task<bool> HasSubCategoriesAsync(int mainCategoryId)
        {
            return await _context.SubCategories.AnyAsync(x => x.MainCategoryId == mainCategoryId);
        }

        async public Task<bool> DeleteMainCategoryAsync(int id)
        {
            var mainCategory = await _context.MainCategories.FirstOrDefaultAsync(x => x.MainCategoryId == id);
            if (mainCategory == null)
            {
                return false;
            }

            _context.MainCategories.Remove(mainCategory);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
