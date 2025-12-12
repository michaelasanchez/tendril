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

        builder.Property(x => x.SourceField)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TargetField)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TransformType)
            .HasConversion<string>();

        builder.Property(x => x.RegexPattern)
            .HasColumnType("nvarchar(256)");

        builder.Property(x => x.RegexReplacement)
            .HasColumnType("nvarchar(128)");

        builder.Property(x => x.SplitDelimiter)
            .HasColumnType("nvarchar(16)");
    }
}
