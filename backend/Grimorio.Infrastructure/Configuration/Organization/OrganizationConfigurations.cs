using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Grimorio.Domain.Entities.Organization;

namespace Grimorio.Infrastructure.Configuration.Organization;

public class BranchConfiguration : BaseEntityConfiguration<Branch>
{
    public override void Configure(EntityTypeBuilder<Branch> builder)
    {
        base.Configure(builder);

        builder.ToTable("Branches", "organization");

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.IdentificationNumber)
            .HasMaxLength(13)
            .IsRequired();

        builder.Property(s => s.Address)
            .HasMaxLength(500);

        builder.Property(s => s.Phone)
            .HasMaxLength(20);

        builder.Property(s => s.Email)
            .HasMaxLength(256);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        // Ubicación geográfica
        builder.Property(b => b.Latitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        builder.Property(b => b.Longitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        // Índices
        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.IsActive);

        // Relaciones
        builder.HasMany(b => b.Positions)
            .WithOne(p => p.Branch)
            .HasForeignKey(p => p.BranchIdParent)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Employees)
            .WithOne(e => e.Branch)
              .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PositionConfiguration : BaseEntityConfiguration<Position>
{
    public override void Configure(EntityTypeBuilder<Position> builder)
    {
        base.Configure(builder);

        builder.ToTable("Positions", "organization");

        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.BranchIdParent)
            .HasColumnName("BranchIdParent")
            .IsRequired();

        // Índices
        builder.HasIndex(p => new { p.BranchIdParent, p.Name }).IsUnique();

        // Relaciones
        builder.HasOne(p => p.Branch)
            .WithMany(b => b.Positions)
            .HasForeignKey(p => p.BranchIdParent)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Employees)
            .WithOne(e => e.Position)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeConfiguration : BaseEntityConfiguration<Employee>
{
    public override void Configure(EntityTypeBuilder<Employee> builder)
    {
        base.Configure(builder);

        builder.ToTable("Employees", "organization");

        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(256);

        builder.Property(e => e.Phone)
            .HasMaxLength(20);

        builder.Property(e => e.IdentificationNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.HireDate)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // Configuración de contrato
        builder.Property(e => e.ContractType)
            .HasDefaultValue(Domain.Enums.ContractType.FullTime)
            .IsRequired();

        builder.Property(e => e.WeeklyMinHours)
            .HasPrecision(5, 2)
            .HasDefaultValue(40m)
            .IsRequired();

        builder.Property(e => e.WeeklyMaxHours)
            .HasPrecision(5, 2)
            .HasDefaultValue(40m)
            .IsRequired();

        builder.Property(e => e.FreeDaysPerMonth)
            .HasDefaultValue(6)
            .IsRequired();

        builder.Property(e => e.BaseSalary)
            .HasPrecision(12, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(e => e.BankAccount)
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(e => e.DecimoThirdMonthly)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.DecimoFourthMonthly)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.ReserveFundMonthly)
            .HasDefaultValue(false)
            .IsRequired();

        // Información personal adicional
        builder.Property(e => e.Photo)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(e => e.DateOfBirth)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(e => e.CivilStatus)
            .HasMaxLength(50)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(e => e.Sex)
            .HasMaxLength(20)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(e => e.Nationality)
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        // Información de contacto de emergencia
        builder.Property(e => e.EmergencyContactPerson)
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(e => e.EmergencyContactRelationship)
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(e => e.EmergencyContactPhone)
            .HasMaxLength(20)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        // Índices
        builder.HasIndex(e => new { e.BranchId, e.IdentificationNumber }).IsUnique();
        builder.HasIndex(e => new { e.BranchId, e.IsActive });

        // Relaciones
        builder.HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Employees)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.EmployeeShifts)
            .WithOne(es => es.Employee)
            .HasForeignKey(es => es.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.EmployeeClockings)
            .WithOne(ec => ec.Employee)
            .HasForeignKey(ec => ec.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmployeeShiftConfiguration : BaseEntityConfiguration<EmployeeShift>
{
    public override void Configure(EntityTypeBuilder<EmployeeShift> builder)
    {
        base.Configure(builder);

        builder.ToTable("EmployeeShifts", "organization");

        builder.Property(es => es.EmployeeId)
            .IsRequired();

        builder.Property(es => es.ShiftName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(es => es.StartTime)
            .IsRequired();

        builder.Property(es => es.EndTime)
            .IsRequired();

        builder.Property(es => es.IsActive)
            .HasDefaultValue(true);

        // Índices
        builder.HasIndex(es => new { es.EmployeeId, es.IsActive });

        // Relaciones
        builder.HasOne(es => es.Employee)
            .WithMany(e => e.EmployeeShifts)
            .HasForeignKey(es => es.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmployeeClockingConfiguration : BaseEntityConfiguration<EmployeeClocking>
{
    public override void Configure(EntityTypeBuilder<EmployeeClocking> builder)
    {
        base.Configure(builder);

        builder.ToTable("EmployeeClockings", "organization");

        builder.Property(ec => ec.EmployeeId)
            .IsRequired();

        builder.Property(ec => ec.ClockInTime)
            .IsRequired();

        builder.Property(ec => ec.Notes)
            .HasMaxLength(500);

        // Índices
        builder.HasIndex(ec => new { ec.EmployeeId, ec.ClockInTime });
        builder.HasIndex(ec => new { ec.BranchId, ec.ClockInTime });

        // Relaciones
        builder.HasOne(ec => ec.Employee)
            .WithMany(e => e.EmployeeClockings)
            .HasForeignKey(ec => ec.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
