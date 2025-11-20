using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

public class ScraperAttemptHistoryConfig : IEntityTypeConfiguration<ScraperAttemptHistory>
{
    public void Configure(EntityTypeBuilder<ScraperAttemptHistory> builder)
    {
        builder.ToTable("ScraperAttemptHistory");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);
    }
}
