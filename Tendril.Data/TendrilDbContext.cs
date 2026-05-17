using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Data.Configurations;

namespace Tendril.Data;

public class TendrilDbContext(DbContextOptions<TendrilDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRevision> EventRevisions => Set<EventRevision>();
    public DbSet<EventTag> EventTags => Set<EventTag>();
    public DbSet<RuleAssignment> RuleAssignments => Set<RuleAssignment>();
    public DbSet<ScrapedEventRaw> RawEvents => Set<ScrapedEventRaw>();
    public DbSet<ApiParameter> ApiParameters => Set<ApiParameter>();
    public DbSet<ScraperAttemptHistory> AttemptHistory => Set<ScraperAttemptHistory>();
    public DbSet<ScraperClassificationRule> ClassificationRules => Set<ScraperClassificationRule>();
    public DbSet<ScraperMappingRule> MappingRules => Set<ScraperMappingRule>();
    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();
    public DbSet<ScraperDefinition> Scrapers => Set<ScraperDefinition>();
    public DbSet<ScraperAction> Actions => Set<ScraperAction>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Venue> Venues => Set<Venue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ApiParameterConfig());
        modelBuilder.ApplyConfiguration(new CategoryConfig());
        modelBuilder.ApplyConfiguration(new EventConfig());
        modelBuilder.ApplyConfiguration(new EventRevisionConfig());
        modelBuilder.ApplyConfiguration(new EventTagConfig());
        modelBuilder.ApplyConfiguration(new RuleAssignmentConfig());
        modelBuilder.ApplyConfiguration(new ScheduledTaskConfig());
        modelBuilder.ApplyConfiguration(new ScrapedEventRawConfig());
        modelBuilder.ApplyConfiguration(new ScraperAttemptHistoryConfig());
        modelBuilder.ApplyConfiguration(new ScraperClassificationRuleConfig());
        modelBuilder.ApplyConfiguration(new ScraperDefinitionConfig());
        modelBuilder.ApplyConfiguration(new ScraperMappingRuleConfig());
        modelBuilder.ApplyConfiguration(new ScraperActionConfig());
        modelBuilder.ApplyConfiguration(new TagConfig());
        modelBuilder.ApplyConfiguration(new UserConfig());
        modelBuilder.ApplyConfiguration(new VenueConfig());
    }
}
