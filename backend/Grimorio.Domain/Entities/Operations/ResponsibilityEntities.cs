using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Operations;

public enum ResponsibilityFrequency { Daily, Weekly, Fortnightly, Monthly, EveryNDays }
public enum ResponsibilityStage { Opening, DuringShift, Closing }
public enum ResponsibilityPhotoMode { None, After, BeforeAndAfter }
public enum ResponsibilityStatus { Pending, Completed, CompletedWithIssue }

public class ResponsibilityArea : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>Operational position (Mesera 1), distinct from access roles and payroll positions.</summary>
public class ResponsibilityPosition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ResponsibilityTask : BaseEntity
{
    public Guid AreaId { get; set; }
    public ResponsibilityArea? Area { get; set; }
    public Guid PositionId { get; set; }
    public ResponsibilityPosition? Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public ResponsibilityFrequency Frequency { get; set; }
    public ResponsibilityStage Stage { get; set; }
    public ResponsibilityPhotoMode PhotoMode { get; set; }
    public DateOnly AnchorDate { get; set; }
    public int DaysOfWeekMask { get; set; } = 127;
    public int EveryNDays { get; set; } = 1;
    public int? IntervalMinutes { get; set; }
    public int WindowMinutes { get; set; } = 30;
    public string ChecklistJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
}

/// <summary>A weekly rotation matches a position to the employee's existing scheduled shift start.</summary>
public class ResponsibilityAssignment : BaseEntity
{
    public Guid PositionId { get; set; }
    public ResponsibilityPosition? Position { get; set; }
    public Guid EmployeeId { get; set; }
    public Organization.Employee? Employee { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public TimeSpan ShiftStartTime { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Immutable schedule, owner and checklist snapshots; changing a template never rewrites history.</summary>
public class ResponsibilityActivity : BaseEntity
{
    public Guid TaskId { get; set; }
    public ResponsibilityTask? Task { get; set; }
    public Guid AssignmentId { get; set; }
    public ResponsibilityAssignment? Assignment { get; set; }
    public Guid ShiftAssignmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public Organization.Employee? Employee { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public ResponsibilityStage Stage { get; set; }
    public ResponsibilityPhotoMode PhotoMode { get; set; }
    public string ChecklistJson { get; set; } = "[]";
    public DateOnly WorkDate { get; set; }
    public int Sequence { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime ShiftEndsAtUtc { get; set; }
    public ResponsibilityStatus Status { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public Guid? CompletionKey { get; set; }
    public string AnswersJson { get; set; } = "[]";
    public string? Notes { get; set; }
    public ICollection<ResponsibilityEvidence> Evidence { get; set; } = new List<ResponsibilityEvidence>();
}

/// <summary>A one-use, employee-bound camera ticket; storage is private and never served from uploads.</summary>
public class ResponsibilityEvidence : BaseEntity
{
    public Guid ActivityId { get; set; }
    public ResponsibilityActivity? Activity { get; set; }
    public Guid EmployeeId { get; set; }
    public string Phase { get; set; } = "After";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public bool IsCancelled { get; set; }
    public string? StorageKey { get; set; }
    public string? Sha256 { get; set; }
    public long SizeBytes { get; set; }
}

public class ResponsibilityTransfer : BaseEntity
{
    public Guid ActivityId { get; set; }
    public ResponsibilityActivity? Activity { get; set; }
    public Guid FromEmployeeId { get; set; }
    public Guid ToEmployeeId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
