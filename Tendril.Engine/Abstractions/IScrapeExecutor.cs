using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Engine.Models;

namespace Tendril.Engine.Abstractions;

public interface IScrapeExecutor
{
    IAsyncEnumerable<RawScrapedEvent> RunScraperAsync(
        ScraperDefinition def,
        [EnumeratorCancellation] CancellationToken ct);
}
