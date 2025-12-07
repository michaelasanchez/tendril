
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

namespace Tendril.Data.Configurations;

public class ScraperSelectorConfig : IEntityTypeConfiguration<ScraperSelector>
{
    public void Configure(EntityTypeBuilder<ScraperSelector> builder)
    {
        builder.ToTable("ScraperSelector");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Selector)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired();
    }
}
