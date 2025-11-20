using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tendril.Core.Domain.Entities;

public class VenueConfig : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("Venue");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Website)
            .HasMaxLength(500);
    }
}
