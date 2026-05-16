namespace Services;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9093";
    public string Topic { get; set; } = "orders";
}
