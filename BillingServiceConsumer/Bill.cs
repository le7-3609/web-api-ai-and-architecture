namespace BillingConsumer;

public class Bill
{
    public Guid BillId { get; set; } = Guid.NewGuid();
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public double Amount { get; set; }
    public int ItemCount { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public BillStatus Status { get; set; } = BillStatus.Pending;
}

public enum BillStatus
{
    Pending,
    Processed,
    Failed
}
