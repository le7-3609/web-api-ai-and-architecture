using Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public class ProductRepository : IProductRepository
    {
        MyShopContext _context;
        public ProductRepository(MyShopContext context)
        {
            _context = context;
        }

        async public Task<(IEnumerable<Product>, int TotalCount)> GetProductsAsync(int position, int skip, string? desc, int?[] subCategoryIds)
        {
            var ids = GetSubCategoryIds(subCategoryIds);
            var query = BuildProductsQuery(desc, ids);
            var total = await query.CountAsync();
            var products = await GetPagedProductsAsync(query, skip, position);

            return (products, total);
        }

        async public Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                    .Include(p => p.SubCategory) 
                    .FirstOrDefaultAsync(p => p.ProductId == id);
        }

        async public Task<IEnumerable<Product>> GetProductsBySubCategoryIdAsync(int subCategoryId)
        {
            return await _context.Products
                .Where(p => p.SubCategoryId == subCategoryId)
                .ToListAsync();
        }

        async public Task<Product> AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        async public Task UpdateProductAsync(int id, Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        async public Task<bool> DeleteProductAsync(int id)
        {
            var productObjectToDelete = await _context.Products.FirstOrDefaultAsync(product => product.ProductId == id);
            if (productObjectToDelete == null)
            {
                return false;
            }

            _context.Products.Remove(productObjectToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Product>> GetProductsByIdsWithCategoriesAsync(IEnumerable<long> productIds)
        {
            var ids = productIds.ToList();
            return await _context.Products
                .Where(p => ids.Contains(p.ProductId))
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.MainCategory)
                .ToListAsync();
        }

        public async Task<bool> HasOrderItemsByProductIdAsync(int productId)
        {
            return await _context.OrderItems.AnyAsync(oi => oi.ProductId == productId);
        }

        public async Task RemoveCartItemsByProductIdAsync(int productId)
        {
            var cartItems = await _context.CartItems.Where(ci => ci.ProductId == productId).ToListAsync();
            if (cartItems.Count > 0)
            {
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }
        }

        private static long[] GetSubCategoryIds(int?[] subCategoryIds)
        {
            return (subCategoryIds ?? Array.Empty<int?>())
                .Where(id => id.HasValue)
                .Select(id => (long)id!.Value)
                .ToArray();
        }

        private IQueryable<Product> BuildProductsQuery(string? desc, long[] subCategoryIds)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(desc))
            {
                query = query.Where(product => product.ProductName.Contains(desc));
            }

            if (subCategoryIds.Length > 0)
            {
                query = query.Where(product => subCategoryIds.Contains(product.SubCategoryId));
            }

            return query.OrderBy(product => product.ProductName);
        }

        private static Task<List<Product>> GetPagedProductsAsync(IQueryable<Product> query, int skip, int position)
        {
            return query
                .Skip(skip)
                .Take(position)
                .Include(product => product.SubCategory)
                .ToListAsync();
        }

    }
}
