namespace BillingConsumer;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9093";
    public string Topic { get; set; } = "orders";
    public string GroupId { get; set; } = "billing-service";
    public string DeadLetterTopic { get; set; } = "orders.dead-letter";
}
