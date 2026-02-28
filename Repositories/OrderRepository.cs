using Entities;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public class OrderRepository : IOrderRepository
    {
        MyShopContext _context;

        public OrderRepository(MyShopContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders.FirstOrDefaultAsync(order => order.OrderId == id);
        }

        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.StatusNavigation)
                .Include(o => o.BasicSite)
                    .ThenInclude(bs => bs.SiteType)
                .Include(o => o.Reviews)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Platform)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Prompt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Status>> GetStatusesAsync()
        {
            return await _context.Statuses.ToListAsync();
        }

        public async Task<Order> AddOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateStatusAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task<Review> AddReviewAsync(Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return review;  
        }
        
        public async Task<Review?> GetReviewByOrderIdAsync(int orderId)
        {
            return await _context.Reviews.FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<Review> UpdateReviewAsync(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
            return review;
        }
        
        public async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            return await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Include(oi => oi.Product)
                .Include(oi => oi.Platform)
                .Include(oi => oi.Prompt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetAllReviewsAsync()
        {
            return await _context.Reviews
                .Include(r => r.Order)
                    .ThenInclude(o => o.BasicSite)
                        .ThenInclude(bs => bs.SiteType)
                .ToListAsync();
        }
    }
}