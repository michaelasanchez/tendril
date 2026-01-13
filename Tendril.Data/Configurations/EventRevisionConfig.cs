namespace Tendril.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

public class EventRevisionConfig : IEntityTypeConfiguration<EventRevision>
{
    public void Configure(EntityTypeBuilder<EventRevision> builder)
    {
        builder.ToTable("EventRevision");

        builder.HasKey(x => x.Id);

        builder.HasOne(r => r.Event)
            .WithMany()
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.AttemptHistory)
            .WithMany(a => a.Revisions)
            .HasForeignKey(r => r.AttemptHistoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RawEvent)
            .WithMany()
            .HasForeignKey(er => er.RawEventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Reason)
            .HasConversion<string>();
    }
}
