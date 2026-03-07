using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Abstractions;

public interface IMapperService
{
    Event MapEvent(ScraperDefinition scraper, ScrapedEventRaw raw, int? referenceYear);
}
