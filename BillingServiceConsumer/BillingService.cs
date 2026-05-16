using Microsoft.Extensions.Logging;

namespace BillingConsumer;

public interface IBillingService
{
    Task ProcessAsync(Bill bill);
}

public class BillingService : IBillingService
{
    private readonly ILogger<BillingService> _logger;

    public BillingService(ILogger<BillingService> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(Bill bill)
    {
        bill.Status = BillStatus.Processed;

        _logger.LogInformation(
            "Bill {BillId} processed for Order {OrderId} — Site: '{SiteName}', Amount: {Amount:C}, Items: {ItemCount} [Status: {Status}]",
            bill.BillId, bill.OrderId, bill.SiteName, bill.Amount, bill.ItemCount, bill.Status);

        return Task.CompletedTask;
    }
}
