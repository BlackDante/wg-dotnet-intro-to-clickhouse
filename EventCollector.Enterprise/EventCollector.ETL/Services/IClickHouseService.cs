using EventCollector.ETL.Messages;

namespace EventCollector.ETL.Services;

public interface IClickHouseService
{
    Task BatchInsertTripsAsync(IEnumerable<TaxiTripMessage> trips, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}