namespace EventCollector.API.DTOs;

public record EventResponse
{
    public required string EventId { get; init; }
    public required string Status { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
    public string? Message { get; init; }
}