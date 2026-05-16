using BillingConsumer;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<KafkaSettings>(context.Configuration.GetSection("KafkaSettings"));
        services.AddScoped<IBillingService, BillingService>();
        services.AddHostedService<KafkaConsumerService>();
        services.AddHealthChecks()
                .AddCheck<KafkaHealthCheck>("kafka");
    })
    .Build();

await host.RunAsync();
