using Confluent.Kafka;
using DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BillingConsumer;

public class KafkaConsumerService : BackgroundService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;
    private readonly string _deadLetterTopic;
    private readonly IProducer<string, string> _dltProducer;

    private const int MaxRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public KafkaConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> options,
        ILogger<KafkaConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var settings = options.Value;
        _bootstrapServers = settings.BootstrapServers;
        _topic = settings.Topic;
        _groupId = settings.GroupId;
        _deadLetterTopic = settings.DeadLetterTopic;

        var dltConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _dltProducer = new ProducerBuilder<string, string>(dltConfig).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Billing consumer starting. Topic: '{Topic}', Group: '{GroupId}'",
            _topic, _groupId);

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Fix #1: short-timeout poll instead of Task.Run — no thread pool thread wasted,
                // loop stays responsive to cancellation
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null) continue;

                _logger.LogInformation(
                    "Received message [partition {P}, offset {O}]",
                    result.Partition, result.Offset);

                await ProcessWithRetryAsync(result, consumer, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Billing consumer shutting down.");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessWithRetryAsync(
        ConsumeResult<string, string> result,
        IConsumer<string, string> consumer,
        CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var order = JsonSerializer.Deserialize<OrderDetailsDTO>(
                    result.Message.Value, JsonOptions);

                if (order is null)
                {
                    _logger.LogWarning(
                        "[partition {P}, offset {O}] Null deserialization result — sending to DLT.",
                        result.Partition, result.Offset);
                    await SendToDeadLetterAsync(result, "Null deserialization result");
                    consumer.Commit(result);
                    return;
                }

                var bill = new Bill
                {
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    SiteName = order.SiteName ?? string.Empty,
                    Amount = order.OrderSum,
                    ItemCount = order.OrderItemsCount
                };

                // Fix #2: create a fresh scope per message
                await using var scope = _scopeFactory.CreateAsyncScope();
                var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();
                await billingService.ProcessAsync(bill);

                consumer.Commit(result);
                return;
            }
            catch (OperationCanceledException)
            {
                throw; // host is shutting down — do not retry
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                // Fix #3: retry with exponential back-off
                _logger.LogWarning(ex,
                    "[partition {P}, offset {O}] Attempt {Attempt}/{Max} failed — retrying.",
                    result.Partition, result.Offset, attempt, MaxRetries);

                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[partition {P}, offset {O}] Dead-lettering after {Max} attempts.",
                    result.Partition, result.Offset, MaxRetries);

                await SendToDeadLetterAsync(result, ex.Message);
                consumer.Commit(result);
            }
        }
    }

    private async Task SendToDeadLetterAsync(ConsumeResult<string, string> original, string reason)
    {
        var headers = new Headers
        {
            { "x-failed-topic",     System.Text.Encoding.UTF8.GetBytes(_topic) },
            { "x-failed-partition", System.Text.Encoding.UTF8.GetBytes(original.Partition.Value.ToString()) },
            { "x-failed-offset",    System.Text.Encoding.UTF8.GetBytes(original.Offset.Value.ToString()) },
            { "x-failure-reason",   System.Text.Encoding.UTF8.GetBytes(reason) },
            { "x-failed-at",        System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("o")) }
        };

        try
        {
            await _dltProducer.ProduceAsync(_deadLetterTopic,
                new Message<string, string>
                {
                    Key     = original.Message.Key,
                    Value   = original.Message.Value,
                    Headers = headers
                });

            _logger.LogWarning(
                "[partition {P}, offset {O}] Published to DLT '{DLT}'. Reason: {Reason}",
                original.Partition, original.Offset, _deadLetterTopic, reason);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[partition {P}, offset {O}] FAILED to publish to DLT '{DLT}'. Message will be lost.",
                original.Partition, original.Offset, _deadLetterTopic);
        }
    }

    public override void Dispose()
    {
        _dltProducer.Flush(TimeSpan.FromSeconds(10));
        _dltProducer.Dispose();
        base.Dispose();
    }
}
