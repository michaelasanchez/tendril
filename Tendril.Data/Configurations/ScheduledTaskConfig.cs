using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

namespace Tendril.Data.Configurations;

public class ScheduledTaskConfig : IEntityTypeConfiguration<ScheduledTask>
{
    public void Configure(EntityTypeBuilder<ScheduledTask> builder)
    {
        builder.ToTable("ScheduledTask");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SelectionStrategy)
            .HasConversion<string>();

        builder.Property(x => x.MaxRetries)
            .HasDefaultValue(3);

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasMany(x => x.ScraperDefinitions)
            .WithMany(x => x.ScheduledTasks)
            .UsingEntity(j => j.ToTable("ScheduledTaskScrapers"));
    }
}