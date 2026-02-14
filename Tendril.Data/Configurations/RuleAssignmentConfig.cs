using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

namespace Tendril.Data.Configurations;

public class RuleAssignmentConfig : IEntityTypeConfiguration<RuleAssignment>
{
    public void Configure(EntityTypeBuilder<RuleAssignment> builder)
    {
        builder.ToTable("RuleAssignment");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ScraperClassificationRule)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.ScraperClassificationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}