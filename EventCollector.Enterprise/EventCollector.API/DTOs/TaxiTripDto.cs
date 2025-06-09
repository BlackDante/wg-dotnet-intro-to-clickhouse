namespace EventCollector.API.DTOs;

public record TaxiTripDto
{
    public required int VendorID { get; init; }
    public DateTime tpep_pickup_datetime { get; init; }
    public DateTime tpep_dropoff_datetime { get; init; }
    public int passenger_count { get; init; }
    public decimal trip_distance { get; init; }
    public int RatecodeID { get; init; }
    public char store_and_fwd_flag { get; init; } = 'N';
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
    
    // Event metadata
    public string? source { get; init; } = "api";
    public string? correlationId { get; init; }
    public Dictionary<string, string>? metadata { get; init; }
}