namespace Tendril.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

public class ApiParameterConfig : IEntityTypeConfiguration<ApiParameter>
{
    public void Configure(EntityTypeBuilder<ApiParameter> builder)
    {
        builder.ToTable("ApiParameter");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Target)
            .HasConversion<string>()
            .IsRequired();
    }
}
