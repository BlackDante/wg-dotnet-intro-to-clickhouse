namespace EventCollector.API.DTOs;

public record TaxiEventDto
{
    public required string eventType { get; init; } // "trip.started", "trip.completed", "trip.cancelled"
    public required string source { get; init; } // "taxi-app", "dispatch-system", "payment-service"
    public required TaxiTripDto tripData { get; init; }
    public DateTime timestamp { get; init; } = DateTime.UtcNow;
    public string? correlationId { get; init; }
    public string? driverId { get; init; }
    public string? vehicleId { get; init; }
    public Dictionary<string, string>? metadata { get; init; }
}