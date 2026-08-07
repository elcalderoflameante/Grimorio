using Grimorio.Application.DTOs;
using Grimorio.Domain.Enums;
using MediatR;

namespace Grimorio.Application.Features.Attendance.Commands;

public abstract class KioskAttendanceCommand : IRequest<AttendanceStatusDto>
{
    public Guid EmployeeId { get; set; }
    public Guid KioskDeviceId { get; set; }
    public AttendanceMethod Method { get; set; } = AttendanceMethod.Face;
    public string? EvidencePath { get; set; }
}

public sealed class ClockInCommand : KioskAttendanceCommand { }
public sealed class StartBreakCommand : KioskAttendanceCommand { }
public sealed class EndBreakCommand : KioskAttendanceCommand { }
public sealed class ClockOutCommand : KioskAttendanceCommand { }

public sealed class RegisterKioskCommand : IRequest<KioskRegistrationDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
}

public sealed class RevokeKioskCommand : IRequest<bool>
{
    public Guid KioskId { get; set; }
    public Guid BranchId { get; set; }
}

public sealed class EnrollEmployeeFaceCommand : IRequest<FacialEnrollmentDto>
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
    public Guid EnrolledByUserId { get; set; }
    public List<byte[]> Samples { get; set; } = [];
}

public sealed class RevokeEmployeeFaceCommand : IRequest<bool>
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
}

public sealed class CorrectAttendanceCommand : IRequest<AttendanceAdminRowDto>
{
    public Guid EmployeeClockingId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CorrectedByUserId { get; set; }
    public DateTime ClockInTimeUtc { get; set; }
    public DateTime? ClockOutTimeUtc { get; set; }
    public DateTime? BreakStartedAtUtc { get; set; }
    public DateTime? BreakEndedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateManualAttendanceCommand : IRequest<AttendanceAdminRowDto>
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime ClockInTimeUtc { get; set; }
    public DateTime? ClockOutTimeUtc { get; set; }
    public DateTime? BreakStartedAtUtc { get; set; }
    public DateTime? BreakEndedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}
