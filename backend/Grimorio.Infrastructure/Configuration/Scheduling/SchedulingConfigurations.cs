using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Grimorio.Domain.Entities.Scheduling;

namespace Grimorio.Infrastructure.Configuration.Scheduling;

public class WorkAreaConfiguration : IEntityTypeConfiguration<WorkArea>
{
    public void Configure(EntityTypeBuilder<WorkArea> builder)
    {
        builder.ToTable("WorkAreas", "scheduling");
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(x => x.Description)
            .HasMaxLength(500);
        
        builder.Property(x => x.Color)
            .HasMaxLength(7)
            .HasDefaultValue("#808080");
        
        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class WorkRoleConfiguration : IEntityTypeConfiguration<WorkRole>
{
    public void Configure(EntityTypeBuilder<WorkRole> builder)
    {
        builder.ToTable("WorkRoles", "scheduling");
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(x => x.Description)
            .HasMaxLength(500);
        
        builder.Property(x => x.FreeDaysPerMonth)
            .HasDefaultValue(6);
        
        builder.Property(x => x.DailyHoursTarget)
            .HasPrecision(4, 2)
            .HasDefaultValue(8.0m);
        
        builder.HasOne(x => x.WorkArea)
            .WithMany(x => x.WorkRoles)
            .HasForeignKey(x => x.WorkAreaId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => new { x.WorkAreaId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class EmployeeWorkRoleConfiguration : IEntityTypeConfiguration<EmployeeWorkRole>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkRole> builder)
    {
        builder.ToTable("EmployeeWorkRoles", "scheduling");
        
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.WorkRole)
            .WithMany(x => x.EmployeeWorkRoles)
            .HasForeignKey(x => x.WorkRoleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => new { x.EmployeeId, x.WorkRoleId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class ShiftTemplateConfiguration : IEntityTypeConfiguration<ShiftTemplate>
{
    public void Configure(EntityTypeBuilder<ShiftTemplate> builder)
    {
        builder.ToTable("ShiftTemplates", "scheduling");
        
        builder.Property(x => x.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20);
        
        builder.Property(x => x.Notes)
            .HasMaxLength(1000);
        
        builder.HasOne(x => x.WorkArea)
            .WithMany(x => x.ShiftTemplates)
            .HasForeignKey(x => x.WorkAreaId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.WorkRole)
            .WithMany()
            .HasForeignKey(x => x.WorkRoleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => new { x.BranchId, x.DayOfWeek, x.WorkAreaId, x.WorkRoleId })
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("ShiftAssignments", "scheduling");
        
        builder.Property(x => x.Date)
            .HasColumnType("date");
        
        builder.Property(x => x.WorkedHours)
            .HasPrecision(4, 2);
        
        builder.Property(x => x.Notes)
            .HasMaxLength(1000);
        
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.WorkArea)
            .WithMany()
            .HasForeignKey(x => x.WorkAreaId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.WorkRole)
            .WithMany()
            .HasForeignKey(x => x.WorkRoleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => new { x.EmployeeId, x.Date })
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(x => new { x.Date, x.WorkAreaId, x.WorkRoleId })
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class EmployeeAvailabilityConfiguration : IEntityTypeConfiguration<EmployeeAvailability>
{
    public void Configure(EntityTypeBuilder<EmployeeAvailability> builder)
    {
        builder.ToTable("EmployeeAvailability", "scheduling");
        
        builder.Property(x => x.UnavailableDate)
            .HasColumnType("date");
        
        builder.Property(x => x.Reason)
            .HasMaxLength(500);
        
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => new { x.EmployeeId, x.UnavailableDate })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class ScheduleConfigurationConfiguration : IEntityTypeConfiguration<ScheduleConfiguration>
{
    public void Configure(EntityTypeBuilder<ScheduleConfiguration> builder)
    {
        builder.ToTable("ScheduleConfigurations", "scheduling");
        
        builder.Property(x => x.MinHoursPerMonth)
            .HasPrecision(5, 2)
            .HasDefaultValue(160m);
        
        builder.Property(x => x.MaxHoursPerMonth)
            .HasPrecision(5, 2)
            .HasDefaultValue(220m);
        
        builder.Property(x => x.HoursMondayThursday)
            .HasPrecision(4, 2)
            .HasDefaultValue(8.5m);
        
        builder.Property(x => x.HoursFridaySaturday)
            .HasPrecision(5, 2)
            .HasDefaultValue(12.5m);
        
        builder.Property(x => x.HoursSunday)
            .HasPrecision(4, 2)
            .HasDefaultValue(10m);
        
        builder.Property(x => x.MinStaffCocina)
            .HasDefaultValue(2);
        
        builder.Property(x => x.MinStaffCaja)
            .HasDefaultValue(1);
        
        builder.Property(x => x.MinStaffMesas)
            .HasDefaultValue(3);
        
        builder.Property(x => x.MinStaffBar)
            .HasDefaultValue(1);
        
        builder.HasIndex(x => x.BranchId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
