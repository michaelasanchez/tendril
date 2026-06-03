using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

namespace Tendril.Data.Configurations;

public class ScheduledTaskRunConfig : IEntityTypeConfiguration<ScheduledTaskRun>
{
    public void Configure(EntityTypeBuilder<ScheduledTaskRun> builder)
    {
        builder.ToTable("ScheduledTaskRun");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.HasMany(x => x.AttemptHistories)
            .WithOne(x => x.ScheduledTaskRun)
            .HasForeignKey(x => x.ScheduledTaskRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
