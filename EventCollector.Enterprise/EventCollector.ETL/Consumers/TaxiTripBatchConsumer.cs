using EventCollector.ETL.Messages;
using EventCollector.ETL.Services;
using MassTransit;

namespace EventCollector.ETL.Consumers;

public class TaxiTripBatchConsumer : IConsumer<Batch<TaxiTripMessage>>
{
    private readonly IClickHouseService _clickHouseService;
    private readonly ILogger<TaxiTripBatchConsumer> _logger;

    public TaxiTripBatchConsumer(IClickHouseService clickHouseService, ILogger<TaxiTripBatchConsumer> logger)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Batch<TaxiTripMessage>> context)
    {
        var batch = context.Message;
        var trips = batch.Select(x => x.Message).ToList();

        _logger.LogInformation("Processing batch of {Count} taxi trips", trips.Count);

        try
        {
            await _clickHouseService.BatchInsertTripsAsync(trips, context.CancellationToken);
            
            _logger.LogInformation("Successfully processed batch of {Count} taxi trips", trips.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process batch of {Count} taxi trips", trips.Count);
            throw; // This will cause the message to be retried or moved to error queue
        }
    }
}