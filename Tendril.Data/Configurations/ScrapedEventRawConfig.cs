namespace Tendril.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

public class ScrapedEventRawConfig : IEntityTypeConfiguration<ScrapedEventRaw>
{
    public void Configure(EntityTypeBuilder<ScrapedEventRaw> builder)
    {
        builder.ToTable("ScrapedEventRaw");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RawDataJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.HasOne(raw => raw.ScraperAttemptHistory)
            .WithMany(evt => evt.ScrapedEventRaws)
            .HasForeignKey(raw => raw.ScraperAttemptHistoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(raw => raw.Event)
            .WithMany(evt => evt.ScrapedEventRaws)
            .HasForeignKey(raw => raw.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
