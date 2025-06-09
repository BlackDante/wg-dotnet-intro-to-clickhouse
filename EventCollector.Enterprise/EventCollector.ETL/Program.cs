using EventCollector.ETL.Consumers;
using EventCollector.ETL.Messages;
using EventCollector.ETL.Services;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddScoped<IClickHouseService, ClickHouseService>();

// Configure MassTransit with Kafka
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();

    x.AddRider(rider =>
    {
        rider.AddConsumer<TaxiTripBatchConsumer>();

        rider.UsingKafka((context, k) =>
        {
            var kafkaConfig = builder.Configuration.GetSection("Kafka");
            k.Host(kafkaConfig["BootstrapServers"]);

            k.TopicEndpoint<TaxiTripMessage>(kafkaConfig["Topic"], kafkaConfig["ConsumerGroup"], e =>
            {
                e.PrefetchCount = 10_000;
                e.ConcurrentMessageLimit = 8_192;
                e.ConcurrentDeliveryLimit = 8_192;
                
                e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                
                e.ConfigureConsumer<TaxiTripBatchConsumer>(context, cons =>
                {
                    cons.Options<BatchOptions>(options => 
                        options.SetMessageLimit(8_192)
                            .SetTimeLimit(1_000));
                });
            });
        });
    });
});

var host = builder.Build();

// Test ClickHouse connection on startup
using (var scope = host.Services.CreateScope())
{
    var clickHouseService = scope.ServiceProvider.GetRequiredService<IClickHouseService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var isConnected = await clickHouseService.TestConnectionAsync();
        if (isConnected)
        {
            logger.LogInformation("ClickHouse connection successful");
        }
        else
        {
            logger.LogError("ClickHouse connection failed");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error testing ClickHouse connection");
    }
}

host.Run();
