using DTO;

namespace Services;

public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(OrderDetailsDTO order);
}
