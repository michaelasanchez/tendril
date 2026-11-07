using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

namespace Tendril.Data.Configurations;

public class WorkerSessionConfig : IEntityTypeConfiguration<WorkerSession>
{
    public void Configure(EntityTypeBuilder<WorkerSession> builder)
    {
        builder.ToTable("WorkerSession");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MachineName)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(x => x.StartedAtUtc);
        builder.HasIndex(x => new { x.MachineName, x.StartedAtUtc });
    }
}