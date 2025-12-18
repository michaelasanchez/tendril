using Tendril.Core.Domain.Entities;
using Tendril.Engine.Models;

namespace Tendril.Engine.Abstractions;

public interface IEventIngestionService
{
    public Task<IngestResult> Ingest(ScraperDefinition scraper, CancellationToken cancellationToken = default);
}
