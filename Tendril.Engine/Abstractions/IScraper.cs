using Tendril.Engine.Models;

namespace Tendril.Engine.Abstractions;

public interface IScraper
{
    Task<ScrapeResult> ExecuteAsync(bool selectorsOnly, CancellationToken cancellationToken = default);
}
