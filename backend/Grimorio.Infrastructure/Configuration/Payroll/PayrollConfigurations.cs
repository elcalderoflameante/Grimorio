using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Grimorio.Domain.Entities.Payroll;

namespace Grimorio.Infrastructure.Configuration.Payroll;

public class PayrollConfigurationConfiguration : IEntityTypeConfiguration<PayrollConfiguration>
{
    public void Configure(EntityTypeBuilder<PayrollConfiguration> builder)
    {
        builder.ToTable("PayrollConfigurations", "payroll");

        builder.Property(x => x.IessEmployeeRate).HasPrecision(5, 2);
        builder.Property(x => x.IessEmployerRate).HasPrecision(5, 2);
        builder.Property(x => x.IncomeTaxRate).HasPrecision(5, 2);
        builder.Property(x => x.OvertimeRate50).HasPrecision(5, 2);
        builder.Property(x => x.OvertimeRate100).HasPrecision(5, 2);
        builder.Property(x => x.DecimoThirdRate).HasPrecision(5, 2);
        builder.Property(x => x.DecimoFourthRate).HasPrecision(5, 2);
        builder.Property(x => x.ReserveFundRate).HasPrecision(5, 2);

        builder.HasIndex(x => x.BranchId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class PayrollAdvanceConfiguration : IEntityTypeConfiguration<PayrollAdvance>
{
    public void Configure(EntityTypeBuilder<PayrollAdvance> builder)
    {
        builder.ToTable("PayrollAdvances", "payroll");

        builder.Property(x => x.Amount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.Method)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.Date)
            .HasColumnType("date");

        builder.HasIndex(x => new { x.BranchId, x.EmployeeId, x.Date })
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class EmployeeConsumptionConfiguration : IEntityTypeConfiguration<EmployeeConsumption>
{
    public void Configure(EntityTypeBuilder<EmployeeConsumption> builder)
    {
        builder.ToTable("EmployeeConsumptions", "payroll");

        builder.Property(x => x.Amount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.Date)
            .HasColumnType("date");

        builder.HasIndex(x => new { x.BranchId, x.EmployeeId, x.Date })
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class PayrollAdjustmentConfiguration : IEntityTypeConfiguration<PayrollAdjustment>
{
    public void Configure(EntityTypeBuilder<PayrollAdjustment> builder)
    {
        builder.ToTable("PayrollAdjustments", "payroll");

        builder.Property(x => x.Amount)
            .HasPrecision(12, 2);

        builder.Property(x => x.Hours)
            .HasPrecision(6, 2);

        builder.Property(x => x.Date)
            .HasColumnType("date");

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.BranchId, x.EmployeeId, x.Date })
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class PayrollRoleHeaderConfiguration : IEntityTypeConfiguration<PayrollRoleHeader>
{
    public void Configure(EntityTypeBuilder<PayrollRoleHeader> builder)
    {
        builder.ToTable("PayrollRoleHeaders", "payroll");

        builder.Property(x => x.TotalIncome)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.TotalDeductions)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.NetPay)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.HasIndex(x => new { x.BranchId, x.EmployeeId, x.Year, x.Month })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(x => x.Details)
            .WithOne(x => x.PayrollRoleHeader)
            .HasForeignKey(x => x.PayrollRoleHeaderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PayrollRoleDetailConfiguration : IEntityTypeConfiguration<PayrollRoleDetail>
{
    public void Configure(EntityTypeBuilder<PayrollRoleDetail> builder)
    {
        builder.ToTable("PayrollRoleDetails", "payroll");

        builder.Property(x => x.Concept)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.HasIndex(x => new { x.PayrollRoleHeaderId, x.SortOrder })
            .HasFilter("\"IsDeleted\" = false");
    }
}
