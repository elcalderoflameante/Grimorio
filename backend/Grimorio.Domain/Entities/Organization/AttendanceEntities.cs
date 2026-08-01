using Grimorio.Domain.Enums;
using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Organization;

/// <summary>Jornada diaria. WorkDate representa el día calendario de Ecuador (UTC-5).</summary>
public class EmployeeClocking : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime ClockInTimeUtc { get; set; }
    public DateTime? ClockOutTimeUtc { get; set; }
    public TimeSpan? ScheduledStartTime { get; set; }
    public TimeSpan? ScheduledEndTime { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Working;
    public AttendanceMethod ClockInMethod { get; set; }
    public AttendanceMethod? ClockOutMethod { get; set; }
    public Guid? ClockInKioskDeviceId { get; set; }
    public Guid? ClockOutKioskDeviceId { get; set; }
    public string? ClockInEvidencePath { get; set; }
    public string? ClockOutEvidencePath { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyArrivalMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string? AdministrativeNotes { get; set; }
    public Employee? Employee { get; set; }
    public EmployeeClockingBreak? Break { get; set; }
}

/// <summary>Único descanso permitido en una jornada; el máximo empresarial es 30 minutos.</summary>
public class EmployeeClockingBreak : BaseEntity
{
    public Guid EmployeeClockingId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public AttendanceMethod StartMethod { get; set; }
    public AttendanceMethod? EndMethod { get; set; }
    public Guid? StartKioskDeviceId { get; set; }
    public Guid? EndKioskDeviceId { get; set; }
    public string? StartEvidencePath { get; set; }
    public string? EndEvidencePath { get; set; }
    public int DurationMinutes { get; set; }
    public bool ClosedAutomaticallyOnClockOut { get; set; }
    public EmployeeClocking? EmployeeClocking { get; set; }
}

/// <summary>Tablet autorizada para operar como kiosco de asistencia.</summary>
public class AttendanceKioskDevice : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public KioskDeviceStatus Status { get; set; } = KioskDeviceStatus.Pending;
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? AppVersion { get; set; }
}

/// <summary>Plantilla facial cifrada y versionada.</summary>
public class EmployeeFacialTemplate : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public string EncryptedEmbedding { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public int EmbeddingDimensions { get; set; }
    public int SampleCount { get; set; }
    public DateTime EnrolledAtUtc { get; set; }
    public Guid EnrolledByUserId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Employee? Employee { get; set; }
}

/// <summary>Registro inmutable de una corrección administrativa de asistencia.</summary>
public class AttendanceCorrection : BaseEntity
{
    public Guid EmployeeClockingId { get; set; }
    public Guid CorrectedByUserId { get; set; }
    public DateTime CorrectedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = string.Empty;
    public string AfterJson { get; set; } = string.Empty;
    public EmployeeClocking? EmployeeClocking { get; set; }
}
