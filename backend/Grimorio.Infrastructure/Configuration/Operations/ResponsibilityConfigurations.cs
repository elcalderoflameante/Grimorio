using Grimorio.Domain.Entities.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Operations;

public class ResponsibilityAreaConfiguration : BaseEntityConfiguration<ResponsibilityArea>
{
    public override void Configure(EntityTypeBuilder<ResponsibilityArea> b) { base.Configure(b); b.ToTable("ResponsibilityAreas", "operations"); b.Property(x => x.Name).HasMaxLength(120).IsRequired(); b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = false"); }
}
public class ResponsibilityPositionConfiguration : BaseEntityConfiguration<ResponsibilityPosition>
{
    public override void Configure(EntityTypeBuilder<ResponsibilityPosition> b) { base.Configure(b); b.ToTable("ResponsibilityPositions", "operations"); b.Property(x => x.Name).HasMaxLength(120).IsRequired(); b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = false"); }
}
public class ResponsibilityTaskConfiguration : BaseEntityConfiguration<ResponsibilityTask>
{
    public override void Configure(EntityTypeBuilder<ResponsibilityTask> b)
    {
        base.Configure(b); b.ToTable("ResponsibilityTasks", "operations");
        b.Property(x => x.Name).HasMaxLength(160).IsRequired(); b.Property(x => x.Instructions).HasMaxLength(2000);
        b.Property(x => x.Frequency).HasConversion<string>().HasMaxLength(30); b.Property(x => x.Stage).HasConversion<string>().HasMaxLength(30); b.Property(x => x.PhotoMode).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.ChecklistJson).HasColumnType("jsonb");
        b.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class ResponsibilityAssignmentConfiguration : BaseEntityConfiguration<ResponsibilityAssignment>
{
    public override void Configure(EntityTypeBuilder<ResponsibilityAssignment> b)
    {
        base.Configure(b); b.ToTable("ResponsibilityAssignments", "operations");
        b.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BranchId, x.PositionId, x.FromDate, x.ToDate });
    }
}
public class ResponsibilityActivityConfiguration : BaseEntityConfiguration<ResponsibilityActivity>
{
    public override void Configure(EntityTypeBuilder<ResponsibilityActivity> b)
    {
        base.Configure(b); b.ToTable("ResponsibilityActivities", "operations");
        b.Property(x => x.EmployeeName).HasMaxLength(160); b.Property(x => x.PositionName).HasMaxLength(120); b.Property(x => x.AreaName).HasMaxLength(120); b.Property(x => x.TaskName).HasMaxLength(160); b.Property(x => x.Instructions).HasMaxLength(2000); b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.Stage).HasConversion<string>().HasMaxLength(30); b.Property(x => x.PhotoMode).HasConversion<string>().HasMaxLength(30); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.ChecklistJson).HasColumnType("jsonb"); b.Property(x => x.AnswersJson).HasColumnType("jsonb");
        b.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Assignment).WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Grimorio.Domain.Entities.Scheduling.ShiftAssignment>().WithMany().HasForeignKey(x => x.ShiftAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BranchId, x.TaskId, x.ShiftAssignmentId, x.Sequence }).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.TaskId, x.WorkDate, x.AvailableAtUtc }).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.EmployeeId, x.WorkDate });
        b.HasIndex(x => new { x.BranchId, x.WorkDate, x.Status });
    }
}
public class ResponsibilityEvidenceConfiguration : BaseEntityConfiguration<ResponsibilityEvidence>
{
    public override void Configure(EntityTypeBuilder<ResponsibilityEvidence> b)
    {
        base.Configure(b); b.ToTable("ResponsibilityEvidence", "operations");
        b.Property(x => x.Phase).HasMaxLength(10); b.Property(x => x.StorageKey).HasMaxLength(100); b.Property(x => x.Sha256).HasMaxLength(64);
        b.HasOne(x => x.Activity).WithMany(x => x.Evidence).HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class ResponsibilityTransferConfiguration : BaseEntityConfiguration<ResponsibilityTransfer>
{
    public override void Configure(EntityTypeBuilder<ResponsibilityTransfer> b)
    {
        base.Configure(b); b.ToTable("ResponsibilityTransfers", "operations"); b.Property(x => x.Reason).HasMaxLength(500);
        b.HasOne(x => x.Activity).WithMany().HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Restrict);
    }
}
