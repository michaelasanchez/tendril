
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

namespace Tendril.Data.Configurations;

public class ScraperActionConfig : IEntityTypeConfiguration<ScraperAction>
{
    public void Configure(EntityTypeBuilder<ScraperAction> builder)
    {
        builder.ToTable("ScraperAction");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Selector)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne<ScraperDefinition>()
            .WithMany(x => x.ParentSelectors)
            .HasForeignKey(x => x.ChildScraperDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
