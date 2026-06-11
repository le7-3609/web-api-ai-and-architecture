using Entities;

namespace Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<Order?> GetByIdWithItemsAsync(int id);
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<IEnumerable<Status>> GetStatusesAsync();
        Task<Order> AddOrderAsync(Order order);
        Task UpdateStatusAsync(Order order);
        Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);
    }
}