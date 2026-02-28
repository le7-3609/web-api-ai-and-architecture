using Entities;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public class CartRepository : ICartRepository
    {
        MyShopContext _context;

        public CartRepository(MyShopContext shopContext)
        {
            _context = shopContext;
        }

        public async Task<Cart?> GetCartByIdAsync(int cartId)
        {
            return await _context.Carts.FirstOrDefaultAsync(c => c.CartId == cartId);
        }

        public async Task<Cart?> GetCartByUserIdAsync(int userId)
        {
            return await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            return await _context.Users.AnyAsync(u => u.UserId == userId);
        }

        public async Task<bool> ProductExistsAsync(int productId)
        {
            return await _context.Products.AnyAsync(p => p.ProductId == productId);
        }

        public async Task<bool> BasicSiteExistsAsync(int basicSiteId)
        {
            return await _context.BasicSites.AnyAsync(bs => bs.BasicSiteId == basicSiteId);
        }

        public async Task<CartItem?> GetCartItemByIdAsync(int cartItemId)
        {
            return await GetCartItemsWithDetailsQuery()
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
        }

        public async Task<IEnumerable<CartItem>> GetCartItemsByCartIdAsync(int cartId)
        {
            return await GetCartItemsWithDetailsQuery()
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();
        }

        public async Task<Cart> CreateUserCartAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task<CartItem> AddCartItemAsync(CartItem cartItem)
        {
            await _context.CartItems.AddAsync(cartItem);
            await _context.SaveChangesAsync();

            var savedCartItem = await GetCartItemByIdAsync((int)cartItem.CartItemId);
            return savedCartItem ?? cartItem;
        }

        public async Task<CartItem?> UpdateCartItemAsync(CartItem cartItem)
        {
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItem.CartItemId);

            if (existing == null)
            {
                return null;
            }

            existing.PlatformId = cartItem.PlatformId;
            existing.IsActive = cartItem.IsActive;
            await _context.SaveChangesAsync();

            return await GetCartItemByIdAsync((int)cartItem.CartItemId);
        }

        public async Task<Cart?> UpdateCartAsync(Cart cart)
        {
            var existing = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == cart.CartId);
            if (existing == null)
            {
                return null;
            }

            existing.BasicSiteId = cart.BasicSiteId;
            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeleteCartAsync(int cartId)
        {
            await ClearCartItemsAsync(cartId);
            Cart? cart = await GetCartByIdAsync(cartId);
            if (cart == null)
                return false;
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCartItemAsync(int cartItemId)
        {
            CartItem? cartItem = await GetCartItemByIdAsync(cartItemId);
            if (cartItem == null)
                return false;
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearCartItemsAsync(int cartId)
        {
            var cartExists = await _context.Carts.AnyAsync(c => c.CartId == cartId);
            if (!cartExists)
            {
                return false;
            }
            var items = await _context.CartItems
                .Where(ci => ci.CartId == cartId && ci.IsActive == true)
                .ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CartItem?> GetByCartAndProductIdAsync(int cartId, int productId)
        {
            return await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);
        }

        private IQueryable<CartItem> GetCartItemsWithDetailsQuery()
        {
            return _context.CartItems
                .Include(ci => ci.Product)
                .ThenInclude(p => p.SubCategory)
                .Include(ci => ci.Platform)
                .Include(ci => ci.Prompt);
        }
    }
}