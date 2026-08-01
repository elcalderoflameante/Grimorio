using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Attendance.Queries;

public sealed class GetTodayAttendanceStatusQuery : IRequest<AttendanceStatusDto>
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
}

public sealed class GetAttendanceKiosksQuery : IRequest<List<AttendanceKioskDto>>
{
    public Guid BranchId { get; set; }
}

public sealed class IdentifyEmployeeFaceQuery : IRequest<FaceIdentificationDto>
{
    public Guid BranchId { get; set; }
    public byte[] Image { get; set; } = [];
}

public sealed class GetFacialEnrollmentsQuery : IRequest<List<FacialEnrollmentDto>>
{
    public Guid BranchId { get; set; }
}

public sealed class GetAttendanceAdminRowsQuery : IRequest<List<AttendanceAdminRowDto>>
{
    public Guid BranchId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public Guid? EmployeeId { get; set; }
}

public sealed class GetAttendanceCorrectionsQuery : IRequest<List<AttendanceCorrectionDto>>
{
    public Guid BranchId { get; set; }
    public Guid EmployeeClockingId { get; set; }
}
