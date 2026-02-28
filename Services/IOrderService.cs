using DTO;
using Entities;

namespace Services
{
    public interface IOrderService
    {
        Task<OrderDetailsDTO?> GetByIdAsync(int id);
        Task<(IEnumerable<OrderDetailsDTO> Orders, double Total)> GetOrdersAsync();
        Task<IEnumerable<StatusesDTO>> GetStatusesAsync();
        Task<OrderDetailsDTO> AddOrderFromCartAsync(int cartId);
        Task UpdateStatusAsync(OrderSummaryDTO dto);
        Task<ReviewDTO?> AddReviewAsync(int orderId, AddReviewDTO dto);
        Task<ReviewDTO?> GetReviewByOrderIdAsync(int orderId);
        Task UpdateReviewAsync(ReviewDTO dto);
        Task<IEnumerable<OrderItemDTO>?> GetOrderItemsAsync(int orderId);
        Task<string?> GetOrderPromptAsync(int orderId);
        Task<IEnumerable<ReviewSummaryDTO>> GetAllReviewsAsync();
    }
}