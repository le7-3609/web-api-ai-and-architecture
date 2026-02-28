using DTO;
using Entities;

namespace Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<IEnumerable<Status>> GetStatusesAsync();
        Task<Order> AddOrderAsync(Order order);
        Task UpdateStatusAsync(Order order); 
        Task<Review> AddReviewAsync(Review review);
        Task<Review?> GetReviewByOrderIdAsync(int orderId);
        Task<Review> UpdateReviewAsync(Review review);
        Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);
        Task<IEnumerable<Review>> GetAllReviewsAsync();
    }
}