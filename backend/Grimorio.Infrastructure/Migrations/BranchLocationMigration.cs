using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Grimorio.Domain.Entities.Organization;

namespace Grimorio.Infrastructure.Configuration.Organization;

public class BranchLocationConfiguration
{
    public static void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.Property(b => b.Latitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        builder.Property(b => b.Longitude)
            .HasPrecision(9, 6)
            .IsRequired(false);
    }
}
