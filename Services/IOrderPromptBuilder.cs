using Entities;

namespace Services
{
    public interface IOrderPromptBuilder
    {
        Task<string> BuildPromptAsync(long basicSiteId, IEnumerable<OrderItem> orderItems);
    }
}
