using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using EventCollector.ETL.Messages;
using System.Data;
using System.Globalization;
using System.Text;

namespace EventCollector.ETL.Services;

public class ClickHouseService : IClickHouseService
{
    private readonly string _connectionString;
    private readonly ILogger<ClickHouseService> _logger;

    public ClickHouseService(IConfiguration configuration, ILogger<ClickHouseService> logger)
    {
        _connectionString = configuration.GetConnectionString("ClickHouse") 
            ?? "Host=localhost;Port=8123;Database=taxi_db;Username=default;Password=";
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            _logger.LogInformation("ClickHouse connection test successful");
            return result?.ToString() == "1";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClickHouse connection test failed");
            return false;
        }
    }

    public async Task BatchInsertTripsAsync(IEnumerable<TaxiTripMessage> trips, CancellationToken cancellationToken = default)
    {
        var tripList = trips.ToList();
        if (!tripList.Any())
        {
            _logger.LogWarning("No trips to insert");
            return;
        }

        try
        {
            using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Use bulk insert SQL instead of ClickHouseBulkCopy for better compatibility
            var insertSql = GenerateBatchInsertSql(tripList);
            
            using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            
            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            
            _logger.LogInformation("Successfully inserted {Count} trips to ClickHouse (rows affected: {RowsAffected})", 
                tripList.Count, rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch insert {Count} trips to ClickHouse", tripList.Count);
            throw;
        }
    }

    private static string GenerateBatchInsertSql(IList<TaxiTripMessage> trips)
    {
        var sql = new StringBuilder();
        sql.AppendLine("INSERT INTO taxi_db.trips (");
        sql.AppendLine("    VendorID, tpep_pickup_datetime, tpep_dropoff_datetime, passenger_count, trip_distance,");
        sql.AppendLine("    RatecodeID, store_and_fwd_flag, PULocationID, DOLocationID, payment_type,");
        sql.AppendLine("    fare_amount, extra, mta_tax, tip_amount, tolls_amount, improvement_surcharge,");
        sql.AppendLine("    total_amount, congestion_surcharge, airport_fee");
        sql.AppendLine(") VALUES");

        for (int i = 0; i < trips.Count; i++)
        {
            var trip = trips[i];
            if (i > 0) sql.AppendLine(",");
            
            // Map to actual ClickHouse table schema
            sql.Append($"({trip.VendorID}, "); // VendorID
            sql.Append($"'{trip.tpep_pickup_datetime:yyyy-MM-dd HH:mm:ss}', "); // tpep_pickup_datetime
            sql.Append($"'{trip.tpep_dropoff_datetime:yyyy-MM-dd HH:mm:ss}', "); // tpep_dropoff_datetime
            sql.Append($"{trip.passenger_count}, "); // passenger_count
            sql.Append($"{trip.trip_distance.ToString(CultureInfo.InvariantCulture)}, "); // trip_distance
            sql.Append($"{trip.RatecodeID}, "); // RatecodeID
            sql.Append($"'{trip.store_and_fwd_flag}', "); // store_and_fwd_flag
            sql.Append($"{trip.PULocationID}, "); // PULocationID
            sql.Append($"{trip.DOLocationID}, "); // DOLocationID
            sql.Append($"{trip.payment_type}, "); // payment_type (as int)
            sql.Append($"{trip.fare_amount.ToString(CultureInfo.InvariantCulture)}, "); // fare_amount
            sql.Append($"{trip.extra.ToString(CultureInfo.InvariantCulture)}, "); // extra
            sql.Append($"{trip.mta_tax.ToString(CultureInfo.InvariantCulture)}, "); // mta_tax
            sql.Append($"{trip.tip_amount.ToString(CultureInfo.InvariantCulture)}, "); // tip_amount
            sql.Append($"{trip.tolls_amount.ToString(CultureInfo.InvariantCulture)}, "); // tolls_amount
            sql.Append($"{trip.improvement_surcharge.ToString(CultureInfo.InvariantCulture)}, "); // improvement_surcharge
            sql.Append($"{trip.total_amount.ToString(CultureInfo.InvariantCulture)}, "); // total_amount
            sql.Append($"{(trip.congestion_surcharge ?? 0).ToString(CultureInfo.InvariantCulture)}, "); // congestion_surcharge
            sql.Append($"{(trip.airport_fee ?? 0).ToString(CultureInfo.InvariantCulture)})"); // airport_fee
        }

        return sql.ToString();
    }
}