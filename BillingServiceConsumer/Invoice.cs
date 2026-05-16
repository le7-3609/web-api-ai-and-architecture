namespace BillingConsumer;

public class Invoice
{
    public Guid InvoiceId { get; set; } = Guid.NewGuid();
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public double Amount { get; set; }
    public int ItemCount { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
}

public enum InvoiceStatus
{
    Pending,
    Processed,
    Failed
}
