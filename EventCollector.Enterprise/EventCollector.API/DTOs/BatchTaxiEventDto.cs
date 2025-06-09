namespace EventCollector.API.DTOs;

public record BatchTaxiEventDto
{
    public required IEnumerable<TaxiEventDto> trips { get; init; }
    public string? batchId { get; init; }
    public string? source { get; init; } = "batch-import";
    public DateTime processingTime { get; init; } = DateTime.UtcNow;
}