using System.ComponentModel.DataAnnotations;

namespace Grimorio.Application.DTOs;

public class ResponsibilityCatalogDto
{
    public List<ResponsibilityNamedDto> Areas { get; set; } = [];
    public List<ResponsibilityNamedDto> Positions { get; set; } = [];
    public List<ResponsibilityEmployeeDto> Employees { get; set; } = [];
    public List<ResponsibilityTaskDto> Tasks { get; set; } = [];
    public List<ResponsibilityAssignmentDto> Assignments { get; set; } = [];
}
public record ResponsibilityEmployeeDto(Guid Id, string Name, bool HasUser);
public class ResponsibilityNamedDto
{
    public Guid Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
public class ResponsibilityCheckDto
{
    [Required, MaxLength(50)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Label { get; set; } = string.Empty;
    public List<string> Options { get; set; } = ["Conforme", "Requiere atención"];
    public List<string> AttentionOptions { get; set; } = [];
}
public class ResponsibilityTaskDto
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }
    public Guid PositionId { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(2000)] public string Instructions { get; set; } = string.Empty;
    public string Frequency { get; set; } = "Daily";
    public string Stage { get; set; } = "Opening";
    public string PhotoMode { get; set; } = "After";
    public DateOnly AnchorDate { get; set; }
    public int DaysOfWeekMask { get; set; } = 127;
    public int EveryNDays { get; set; } = 1;
    public int? IntervalMinutes { get; set; }
    public int WindowMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public List<ResponsibilityCheckDto> Checklist { get; set; } = [];
}
public class ResponsibilityAssignmentDto
{
    public Guid Id { get; set; }
    public Guid PositionId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public TimeSpan ShiftStartTime { get; set; }
    public bool IsActive { get; set; } = true;
}
public class ResponsibilityActivityDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string PhotoMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
    public List<ResponsibilityCheckDto> Checklist { get; set; } = [];
    public List<ResponsibilityAnswerDto> Answers { get; set; } = [];
    public List<ResponsibilityEvidenceDto> Evidence { get; set; } = [];
}
public record ResponsibilityEvidenceDto(Guid Id, string Phase, DateTime? CapturedAtUtc, DateTime? ReceivedAtUtc);
public record ResponsibilityCaptureDto(Guid Id, DateTime IssuedAtUtc, DateTime ExpiresAtUtc);
public record ResponsibilityAnswerDto(string Key, string Value);
public class CompleteResponsibilityDto
{
    public Guid IdempotencyKey { get; set; }
    public bool HasIssue { get; set; }
    [MaxLength(2000)] public string? Notes { get; set; }
    public List<ResponsibilityAnswerDto> Answers { get; set; } = [];
}
public record TransferResponsibilityDto(Guid EmployeeId, [property: Required, MaxLength(500)] string Reason);
public record ResponsibilityScheduleWarningDto(Guid PositionId, string PositionName, DateOnly WorkDate, TimeSpan ShiftStartTime, string Message);
public class ResponsibilityDayDto
{
    public DateOnly WorkDate { get; set; }
    public string? Message { get; set; }
    public List<ResponsibilityActivityDto> Activities { get; set; } = [];
    public List<ResponsibilityScheduleWarningDto> Warnings { get; set; } = [];
}
