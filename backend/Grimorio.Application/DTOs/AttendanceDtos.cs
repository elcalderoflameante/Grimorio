using Grimorio.Domain.Enums;

namespace Grimorio.Application.DTOs;

public sealed class AttendanceStatusDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public AttendanceStatus? Status { get; set; }
    public DateTime? ClockInTimeUtc { get; set; }
    public DateTime? ClockOutTimeUtc { get; set; }
    public DateTime? BreakStartedAtUtc { get; set; }
    public DateTime? BreakEndedAtUtc { get; set; }
    public int BreakMinutes { get; set; }
    public int BreakLimitMinutes { get; set; } = 30;
    public bool BreakLimitExceeded => BreakMinutes > BreakLimitMinutes;
    public int LateMinutes { get; set; }
    public int EarlyArrivalMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
}

public sealed class KioskRegistrationDto
{
    public Guid KioskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class AttendanceKioskDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public string? AppVersion { get; set; }
}

public sealed class FacialEnrollmentDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public int SampleCount { get; set; }
    public DateTime EnrolledAtUtc { get; set; }
}

public sealed class FaceIdentificationDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public double Similarity { get; set; }
}

public sealed class AttendanceAdminRowDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime ClockInTimeUtc { get; set; }
    public DateTime? ClockOutTimeUtc { get; set; }
    public DateTime? BreakStartedAtUtc { get; set; }
    public DateTime? BreakEndedAtUtc { get; set; }
    public int BreakMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyArrivalMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string? AdministrativeNotes { get; set; }
    public int CorrectionCount { get; set; }
}

public sealed class AttendanceCorrectionDto
{
    public Guid Id { get; set; }
    public Guid CorrectedByUserId { get; set; }
    public DateTime CorrectedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = string.Empty;
    public string AfterJson { get; set; } = string.Empty;
}
