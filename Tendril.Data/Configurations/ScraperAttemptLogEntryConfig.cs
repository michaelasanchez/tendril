using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;


namespace Tendril.Data.Configurations;

public class ScraperAttemptLogEntryConfig : IEntityTypeConfiguration<ScraperAttemptLogEntry>
{
    public void Configure(EntityTypeBuilder<ScraperAttemptLogEntry> builder)
    {
        builder.ToTable("ScraperAttemptLogEntry");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Attempt)
            .WithMany(x => x.LogEntries)
            .HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
