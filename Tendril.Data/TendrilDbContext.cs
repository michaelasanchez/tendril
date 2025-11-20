
using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Data.Configurations;

namespace Tendril.Data;


public class TendrilDbContext : DbContext
{
    public TendrilDbContext(DbContextOptions<TendrilDbContext> options)
        : base(options)
    {
    }

    public DbSet<ScraperDefinition> Scrapers => Set<ScraperDefinition>();
    public DbSet<ScraperSelector> Selectors => Set<ScraperSelector>();
    public DbSet<ScraperMappingRule> MappingRules => Set<ScraperMappingRule>();
    public DbSet<ScraperAttemptHistory> AttemptHistory => Set<ScraperAttemptHistory>();
    public DbSet<ScrapedEventRaw> RawEvents => Set<ScrapedEventRaw>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Venue> Venues => Set<Venue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ScraperDefinitionConfig());
        modelBuilder.ApplyConfiguration(new ScraperSelectorConfig());
        modelBuilder.ApplyConfiguration(new ScraperMappingRuleConfig());
        modelBuilder.ApplyConfiguration(new ScraperAttemptHistoryConfig());
        modelBuilder.ApplyConfiguration(new ScrapedEventRawConfig());
        modelBuilder.ApplyConfiguration(new EventConfig());
        modelBuilder.ApplyConfiguration(new VenueConfig());
    }
}
