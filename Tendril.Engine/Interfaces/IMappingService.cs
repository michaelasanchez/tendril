using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Abstractions;

public interface IMappingService
{
    Event MapEvent(ScraperDefinition scraper, ScrapedEventRaw raw, int? referenceYear);
}
