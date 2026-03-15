namespace Tendril.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

public class ScraperDefinitionConfig : IEntityTypeConfiguration<ScraperDefinition>
{
    public void Configure(EntityTypeBuilder<ScraperDefinition> builder)
    {
        builder.ToTable("ScraperDefinition");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.BaseUrl)
            .IsRequired();

        builder.Property(x => x.PaginationType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.ExecutionMode)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.ExtractionStrategy)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.UseYearTracking)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.State)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(1000);

        builder.HasMany(x => x.Selectors)
            .WithOne(x => x.ScraperDefinition)
            .HasForeignKey(x => x.ScraperDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MappingRules)
            .WithOne(x => x.ScraperDefinition)
            .HasForeignKey(x => x.ScraperDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AttemptHistory)
            .WithOne(x => x.ScraperDefinition)
            .HasForeignKey(x => x.ScraperDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Venue)
            .WithMany(v => v.Scrapers)
            .HasForeignKey(x => x.VenueId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
