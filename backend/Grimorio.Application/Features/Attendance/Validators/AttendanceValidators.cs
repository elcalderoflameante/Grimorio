using FluentValidation;
using Grimorio.Application.Features.Attendance.Commands;
using Grimorio.Application.Features.Attendance.Queries;
using Grimorio.Domain.Enums;

namespace Grimorio.Application.Features.Attendance.Validators;

public class KioskAttendanceCommandValidator<T> : AbstractValidator<T> where T : KioskAttendanceCommand
{
    public KioskAttendanceCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.KioskDeviceId).NotEmpty();
        RuleFor(x => x.Method).IsInEnum().NotEqual(AttendanceMethod.Manual);
        RuleFor(x => x.EvidencePath).MaximumLength(500);
    }
}

public sealed class ClockInCommandValidator : KioskAttendanceCommandValidator<ClockInCommand> { }
public sealed class StartBreakCommandValidator : KioskAttendanceCommandValidator<StartBreakCommand> { }
public sealed class EndBreakCommandValidator : KioskAttendanceCommandValidator<EndBreakCommand> { }
public sealed class ClockOutCommandValidator : KioskAttendanceCommandValidator<ClockOutCommand> { }

public sealed class RegisterKioskCommandValidator : AbstractValidator<RegisterKioskCommand>
{
    public RegisterKioskCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DeviceIdentifier).NotEmpty().MaximumLength(200);
    }
}

public sealed class GetTodayAttendanceStatusQueryValidator : AbstractValidator<GetTodayAttendanceStatusQuery>
{
    public GetTodayAttendanceStatusQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public sealed class EnrollEmployeeFaceCommandValidator : AbstractValidator<EnrollEmployeeFaceCommand>
{
    public EnrollEmployeeFaceCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EnrolledByUserId).NotEmpty();
        RuleFor(x => x.Samples).NotNull().Must(x => x.Count == 3)
            .WithMessage("Se requieren exactamente tres muestras faciales.");
        RuleForEach(x => x.Samples).Must(x => x.Length is > 0 and <= 5 * 1024 * 1024)
            .WithMessage("Cada imagen debe tener un tamaño máximo de 5 MB.");
    }
}

public sealed class IdentifyEmployeeFaceQueryValidator : AbstractValidator<IdentifyEmployeeFaceQuery>
{
    public IdentifyEmployeeFaceQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Image).NotEmpty().Must(x => x.Length <= 5 * 1024 * 1024)
            .WithMessage("La imagen debe tener un tamaño máximo de 5 MB.");
    }
}

public sealed class GetAttendanceAdminRowsQueryValidator : AbstractValidator<GetAttendanceAdminRowsQuery>
{
    public GetAttendanceAdminRowsQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x).Must(x => x.ToDate.DayNumber - x.FromDate.DayNumber <= 31)
            .WithMessage("El rango de consulta no puede superar 31 días.");
    }
}

public sealed class CorrectAttendanceCommandValidator : AbstractValidator<CorrectAttendanceCommand>
{
    public CorrectAttendanceCommandValidator()
    {
        RuleFor(x => x.EmployeeClockingId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CorrectedByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(5).MaximumLength(500);
        RuleFor(x => x.ClockInTimeUtc).NotEmpty();
        RuleFor(x => x.ClockOutTimeUtc).GreaterThan(x => x.ClockInTimeUtc)
            .When(x => x.ClockOutTimeUtc.HasValue);
        RuleFor(x => x.BreakEndedAtUtc).GreaterThan(x => x.BreakStartedAtUtc)
            .When(x => x.BreakStartedAtUtc.HasValue && x.BreakEndedAtUtc.HasValue);
        RuleFor(x => x.BreakEndedAtUtc).Null().When(x => !x.BreakStartedAtUtc.HasValue);
    }
}
