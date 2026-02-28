using Microsoft.EntityFrameworkCore;
using Entities;

namespace Repositories
{
    public class SubCategoryRepository : ISubCategoryRepository
    {
        MyShopContext _context;
        public SubCategoryRepository(MyShopContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<SubCategory>, int TotalCount)> GetSubCategoryAsync(int position, int skip, string? desc, int?[] mainCategoryIds)
        {
            var ids = (mainCategoryIds ?? Array.Empty<int?>())
                .Where(id => id.HasValue)
                .Select(id => (long)id!.Value)
                .ToArray();
            var query = _context.SubCategories.AsQueryable();

            if (!string.IsNullOrEmpty(desc))
            {
                query = query.Where(s => s.CategoryDescription.Contains(desc));
            }

            if (ids.Length > 0)
            {
                query = query.Where(s => ids.Contains(s.MainCategoryId));
            }

            query = query.OrderBy(s => s.SubCategoryName);

            var total = await query.CountAsync();

            var subCategories = await query
                .Skip(skip)
                .Take(position)
                .Include(s => s.MainCategory)
                .ToListAsync();

            return (subCategories, total);
        }

        async public Task<SubCategory?> GetSubCategoryByIdAsync(int id)
        {
            return await _context.SubCategories.FirstOrDefaultAsync(x => x.SubCategoryId == id);

        }

        async public Task<SubCategory?> GetByNameAsync(string name)
        {
            return await _context.SubCategories.FirstOrDefaultAsync(x => x.SubCategoryName == name);
        }

        async public Task<bool> MainCategoryExistsAsync(int mainCategoryId)
        {
            return await _context.MainCategories.AnyAsync(x => x.MainCategoryId == mainCategoryId);
        }

        async public Task UpdateSubCategoryAsync(int id, SubCategory category)
        {
            _context.SubCategories.Update(category);
            await _context.SaveChangesAsync();
        }

        async public Task<SubCategory> AddSubCategoryAsync(SubCategory category)
        {
            await _context.SubCategories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category;
        }

        async public Task<bool> HasProductsAsync(int subCategoryId)
        {
            return await _context.Products.AnyAsync(x => x.SubCategoryId == subCategoryId);
        }

        async public Task<bool> DeleteSubCategoryAsync(int id)
        {
            var Category = await _context.SubCategories.FirstOrDefaultAsync(x => x.SubCategoryId == id);
            if (Category == null)
            {
                return false;
            }

            _context.SubCategories.Remove(Category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
