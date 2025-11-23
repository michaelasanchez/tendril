
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

public abstract class BaseScraper : IScraper
{
    public abstract Task<ScrapeResult> ExecuteAsync(bool selectorsOnly, CancellationToken cancellationToken = default);

    protected ScrapeResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };

    protected ScrapeResult Success(List<RawScrapedEvent> events) =>
        new() { Success = true, RawEvents = events };
}
