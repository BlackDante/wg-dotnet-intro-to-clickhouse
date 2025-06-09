namespace EventCollector.API.Messages;

public record EventMessage
{
    public required string EventId { get; init; }
    public required string EventType { get; init; }
    public required string Source { get; init; }
    public required object Data { get; init; }
    public DateTime Timestamp { get; init; }
    public string? CorrelationId { get; init; }
    public string? UserId { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}