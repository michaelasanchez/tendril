using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Interfaces;

public interface IClassificationService
{
    void ClassifyEvent(ScraperDefinition scraper, ScrapedEventRaw mappedEvent, Event targetEvent);
}
