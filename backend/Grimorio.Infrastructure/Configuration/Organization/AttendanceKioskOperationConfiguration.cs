using Grimorio.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Organization;

public sealed class AttendanceKioskOperationConfiguration : BaseEntityConfiguration<AttendanceKioskOperation>
{
    public override void Configure(EntityTypeBuilder<AttendanceKioskOperation> builder)
    {
        base.Configure(builder);
        builder.ToTable("AttendanceKioskOperations", "organization");
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResponseJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.KioskDeviceId, x.CreatedAt });
    }
}
