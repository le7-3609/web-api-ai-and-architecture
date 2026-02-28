using DTO;

namespace Services
{
    public interface ICartService
    {
        Task<bool> ClearCartAsync(int cartId);
        Task<CartItemDTO> AddCartItemForUserAsync(int userId, AddCartItemDTO dto);
        Task<GuestCartImportResultDTO> ImportGuestCartAsync(int userId, ImportGuestCartDTO dto);
        Task<bool> DeleteCartItemAsync(int cartItemId);
        Task<CartDTO?> GetCartByIdAsync(int cartId);
        Task<CartItemDTO?> GetCartItemByIdAsync(int id);
        Task<IEnumerable<CartItemDTO>?> GetCartItemsByCartIdAsync(int cartId);
        Task<CartItemDTO?> UpdateCartItemAsync(UpdateCartItemDTO dto);
        Task<CartDTO?> UpdateCartAsync(int cartId, UpdateCartDTO dto);
    }
}