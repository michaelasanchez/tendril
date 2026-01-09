namespace Tendril.Engine.Models;

public record ScrapeYieldItem
{
    public RawScrapedData Data { get; init; } = new();
    public string? ChildUrl { get; init; }
    public Guid? ChildScraperId { get; init; }
}
