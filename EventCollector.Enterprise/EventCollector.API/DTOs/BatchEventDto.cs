namespace EventCollector.API.DTOs;

public record BatchEventDto
{
    public required IEnumerable<EventDto> Events { get; init; }
    public string? BatchId { get; init; }
}