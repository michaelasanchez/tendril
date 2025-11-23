using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Abstractions;

public interface IScraperFactory
{
    IScraper CreateScraper(ScraperDefinition definition);
}
