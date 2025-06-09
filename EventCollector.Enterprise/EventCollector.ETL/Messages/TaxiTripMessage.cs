namespace EventCollector.ETL.Messages;

public record TaxiTripMessage
{
    public required string eventId { get; init; }
    public required string eventType { get; init; }
    public required string source { get; init; }
    public DateTime timestamp { get; init; }
    
    // Trip data that maps to ClickHouse taxi_db.trips table
    public required int VendorID { get; init; }
    public DateTime tpep_pickup_datetime { get; init; }
    public DateTime tpep_dropoff_datetime { get; init; }
    public int passenger_count { get; init; }
    public decimal trip_distance { get; init; }
    public int RatecodeID { get; init; }
    public char store_and_fwd_flag { get; init; }
    public int PULocationID { get; init; }
    public int DOLocationID { get; init; }
    public int payment_type { get; init; }
    public decimal fare_amount { get; init; }
    public decimal extra { get; init; }
    public decimal mta_tax { get; init; }
    public decimal tip_amount { get; init; }
    public decimal tolls_amount { get; init; }
    public decimal improvement_surcharge { get; init; }
    public decimal total_amount { get; init; }
    public decimal? congestion_surcharge { get; init; }
    public int? trip_type { get; init; }
    public decimal? ehail_fee { get; init; }
    public decimal? airport_fee { get; init; }
    
    // Event metadata
    public string? correlationId { get; init; }
    public string? driverId { get; init; }
    public string? vehicleId { get; init; }
    public Dictionary<string, string>? metadata { get; init; }
}