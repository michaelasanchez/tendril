using Tendril.Core.Domain.Entities;
using Tendril.Engine.Models;

namespace Tendril.Engine.Abstractions;

public interface IIngestionService
{
    public Task<IngestResult> Ingest(ScraperDefinition scraper, CancellationToken cancellationToken = default);
}
