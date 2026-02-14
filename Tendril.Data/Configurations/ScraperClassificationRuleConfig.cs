namespace Tendril.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

public class ScraperClassificationRuleConfig : IEntityTypeConfiguration<ScraperClassificationRule>
{
    public void Configure(EntityTypeBuilder<ScraperClassificationRule> builder)
    {
        builder.ToTable("ScraperClassificationRule");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ScraperDefinition)
            .WithMany(x => x.ClassificationRules)
            .HasForeignKey(x => x.ScraperDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
