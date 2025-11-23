using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Abstractions;

public interface IEventMapper
{
    Event Map(ScraperDefinition scraper, ScrapedEventRaw raw);
}
