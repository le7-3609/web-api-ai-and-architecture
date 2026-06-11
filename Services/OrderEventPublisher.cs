using Confluent.Kafka;
using DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Services;

public class OrderEventPublisher : IOrderEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<OrderEventPublisher> _logger;

    public OrderEventPublisher(IOptions<KafkaSettings> options, ILogger<OrderEventPublisher> logger)
    {
        _logger = logger;
        var settings = options.Value;
        _topic = settings.Topic;

        var config = new ProducerConfig { BootstrapServers = settings.BootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();

        // Probe Kafka connectivity at construction time so failures appear in logs immediately
        try
        {
            using var adminClient = new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = settings.BootstrapServers }).Build();
            adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            _logger.LogInformation("[Startup] Kafka bootstrap probe OK — {BootstrapServers}", settings.BootstrapServers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Startup] Kafka bootstrap probe FAILED — {BootstrapServers}", settings.BootstrapServers);
        }
    }

    public async Task PublishOrderCreatedAsync(OrderDetailsDTO order)
    {
        var payload = JsonSerializer.Serialize(order);

        try
        {
            var result = await _producer.ProduceAsync(
                _topic,
                new Message<string, string>
                {
                    Key = order.OrderId.ToString(),
                    Value = payload
                });

            _logger.LogInformation(
                "Order {OrderId} published to Kafka topic '{Topic}' [partition {Partition}, offset {Offset}]",
                order.OrderId, _topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish Order {OrderId} to Kafka topic '{Topic}'",
                order.OrderId, _topic);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
