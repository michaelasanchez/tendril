using System;
using System.Collections.Generic;
using System.Text;
using Tendril.Core.Domain.Entities;
using Tendril.Data;
using Tendril.Engine.Abstractions;


namespace Tendril.Engine.Runtime;

public class ScraperFactory : IScraperFactory
{
    private readonly TendrilDbContext _db;

    public ScraperFactory(TendrilDbContext db) => _db = db;

    public IScraper CreateScraper(ScraperDefinition def)
    {
        return def.IsDynamic
            ? new DynamicScraper(def, _db)
            : new StaticScraperExample(def);
    }
}
