using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

public class ScraperMappingRuleConfig : IEntityTypeConfiguration<ScraperMappingRule>
{
    public void Configure(EntityTypeBuilder<ScraperMappingRule> builder)
    {
        builder.ToTable("ScraperMappingRule");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InputField)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.OutputField)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TransformType)
            .HasConversion<string>();

        builder.Property(x => x.TransformArgsJson)
            .HasColumnType("nvarchar(max)");
    }
}
