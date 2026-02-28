using Entities;

namespace Repositories
{
    public interface ICartRepository
    {
        Task<CartItem> AddCartItemAsync(CartItem cartItem);
        Task<bool> BasicSiteExistsAsync(int basicSiteId);
        Task<bool> ClearCartItemsAsync(int cartId);
        Task<Cart> CreateUserCartAsync(Cart cart);
        Task<bool> DeleteCartAsync(int cartId);
        Task<bool> DeleteCartItemAsync(int cartItemId);
        Task<Cart?> GetCartByIdAsync(int cartId);
        Task<Cart?> GetCartByUserIdAsync(int userId);
        Task<CartItem?> GetCartItemByIdAsync(int cartItemId);
        Task<IEnumerable<CartItem>> GetCartItemsByCartIdAsync(int cartId);
        Task<bool> ProductExistsAsync(int productId);
        Task<bool> UserExistsAsync(int userId);
        Task<Cart?> UpdateCartAsync(Cart cart);
        Task<CartItem?> UpdateCartItemAsync(CartItem cartItem);
        Task<CartItem?> GetByCartAndProductIdAsync(int cartId, int productId);
    }
}