namespace Tendril.Engine.Models;

public class ScrapeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RawScrapedData> RawEvents { get; set; } = new();
}
