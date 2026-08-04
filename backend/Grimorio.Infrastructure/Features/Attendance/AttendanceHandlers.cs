using System.Security.Cryptography;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Attendance.Commands;
using Grimorio.Application.Features.Attendance.Queries;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Enums;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Attendance;

public sealed class AttendanceHandlers :
    IRequestHandler<ClockInCommand, AttendanceStatusDto>,
    IRequestHandler<StartBreakCommand, AttendanceStatusDto>,
    IRequestHandler<EndBreakCommand, AttendanceStatusDto>,
    IRequestHandler<ClockOutCommand, AttendanceStatusDto>,
    IRequestHandler<GetTodayAttendanceStatusQuery, AttendanceStatusDto>,
    IRequestHandler<GetAttendanceKiosksQuery, List<AttendanceKioskDto>>,
    IRequestHandler<RegisterKioskCommand, KioskRegistrationDto>,
    IRequestHandler<RevokeKioskCommand, bool>,
    IRequestHandler<EnrollEmployeeFaceCommand, FacialEnrollmentDto>,
    IRequestHandler<RevokeEmployeeFaceCommand, bool>,
    IRequestHandler<IdentifyEmployeeFaceQuery, FaceIdentificationDto>,
    IRequestHandler<GetFacialEnrollmentsQuery, List<FacialEnrollmentDto>>,
    IRequestHandler<GetAttendanceAdminRowsQuery, List<AttendanceAdminRowDto>>,
    IRequestHandler<GetAttendanceCorrectionsQuery, List<AttendanceCorrectionDto>>,
    IRequestHandler<CorrectAttendanceCommand, AttendanceAdminRowDto>
{
    public const int BreakLimitMinutes = 30;
    public const double FaceSimilarityThreshold = 0.45;
    public const double FaceAmbiguityMargin = 0.08;
    public const double EnrollmentSampleSimilarityThreshold = 0.45;
    public const double EnrollmentMinimumFaceWidthRatio = 0.18;
    public const double EnrollmentMinimumFaceHeightRatio = 0.30;
    public const double EnrollmentMaximumHorizontalOffset = 0.18;
    public const double EnrollmentMaximumVerticalOffset = 0.20;
    private readonly GrimorioDbContext _context;
    private readonly IPasswordHashingService _passwordHashing;
    private readonly SFaceBiometricService _biometricService;
    private readonly IDataProtector _facialTemplateProtector;

    public AttendanceHandlers(GrimorioDbContext context, IPasswordHashingService passwordHashing,
        SFaceBiometricService biometricService, IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _passwordHashing = passwordHashing;
        _biometricService = biometricService;
        _facialTemplateProtector = dataProtectionProvider.CreateProtector("Grimorio.Attendance.FacialTemplates.v1");
    }

    public async Task<AttendanceStatusDto> Handle(ClockInCommand request, CancellationToken cancellationToken)
    {
        var (employee, kiosk) = await LoadEmployeeAndKiosk(request, cancellationToken);
        var nowUtc = DateTime.UtcNow;
        var timeZoneId = await GetBranchTimeZoneId(kiosk.BranchId, cancellationToken);
        var workDate = BranchTimeZone.DateFromUtc(nowUtc, timeZoneId);

        if (await _context.EmployeeClockings.AnyAsync(
                x => x.EmployeeId == employee.Id && x.WorkDate == workDate && !x.IsDeleted,
                cancellationToken))
            throw new InvalidOperationException("El empleado ya tiene una jornada registrada para hoy.");

        var assignmentDate = workDate.ToDateTime(TimeOnly.MinValue);
        var shift = await _context.ShiftAssignments
            .Where(x => x.EmployeeId == employee.Id && x.Date == assignmentDate && !x.IsDeleted)
            .OrderBy(x => x.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        var localNow = BranchTimeZone.FromUtc(nowUtc, timeZoneId);
        var scheduledStart = shift?.StartTime;
        var delta = scheduledStart.HasValue
            ? (int)Math.Round((localNow.TimeOfDay - scheduledStart.Value).TotalMinutes)
            : 0;

        var clocking = new EmployeeClocking
        {
            BranchId = kiosk.BranchId,
            EmployeeId = employee.Id,
            WorkDate = workDate,
            ClockInTimeUtc = nowUtc,
            ScheduledStartTime = shift?.StartTime,
            ScheduledEndTime = shift?.EndTime,
            Status = AttendanceStatus.Working,
            ClockInMethod = request.Method,
            ClockInKioskDeviceId = kiosk.Id,
            ClockInEvidencePath = NormalizePath(request.EvidencePath),
            LateMinutes = Math.Max(0, delta),
            EarlyArrivalMinutes = Math.Max(0, -delta)
        };

        _context.EmployeeClockings.Add(clocking);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("El empleado ya tiene una jornada registrada para hoy.");
        }
        return Map(employee, clocking);
    }

    public async Task<AttendanceStatusDto> Handle(StartBreakCommand request, CancellationToken cancellationToken)
    {
        var (employee, kiosk) = await LoadEmployeeAndKiosk(request, cancellationToken);
        var clocking = await LoadTodayClocking(employee.Id, kiosk.BranchId, true, cancellationToken);
        if (clocking.Status != AttendanceStatus.Working)
            throw new InvalidOperationException("La jornada no está disponible para iniciar el descanso.");
        if (clocking.Break is not null)
            throw new InvalidOperationException("El descanso permitido para hoy ya fue utilizado.");

        var attendanceBreak = new EmployeeClockingBreak
        {
            BranchId = kiosk.BranchId,
            EmployeeClockingId = clocking.Id,
            StartedAtUtc = DateTime.UtcNow,
            StartMethod = request.Method,
            StartKioskDeviceId = kiosk.Id,
            StartEvidencePath = NormalizePath(request.EvidencePath)
        };
        _context.EmployeeClockingBreaks.Add(attendanceBreak);
        clocking.Break = attendanceBreak;
        clocking.Status = AttendanceStatus.OnBreak;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(employee, clocking);
    }

    public async Task<AttendanceStatusDto> Handle(EndBreakCommand request, CancellationToken cancellationToken)
    {
        var (employee, kiosk) = await LoadEmployeeAndKiosk(request, cancellationToken);
        var clocking = await LoadTodayClocking(employee.Id, kiosk.BranchId, true, cancellationToken);
        if (clocking.Status != AttendanceStatus.OnBreak || clocking.Break?.EndedAtUtc is not null)
            throw new InvalidOperationException("El empleado no tiene un descanso activo.");

        CloseBreak(clocking, DateTime.UtcNow, request.Method, kiosk.Id, request.EvidencePath, false);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(employee, clocking);
    }

    public async Task<AttendanceStatusDto> Handle(ClockOutCommand request, CancellationToken cancellationToken)
    {
        var (employee, kiosk) = await LoadEmployeeAndKiosk(request, cancellationToken);
        var clocking = await LoadTodayClocking(employee.Id, kiosk.BranchId, true, cancellationToken);
        if (clocking.Status == AttendanceStatus.Completed)
            throw new InvalidOperationException("La jornada ya fue finalizada.");

        var nowUtc = DateTime.UtcNow;
        if (clocking.Break is { EndedAtUtc: null })
            CloseBreak(clocking, nowUtc, request.Method, kiosk.Id, request.EvidencePath, true);

        clocking.ClockOutTimeUtc = nowUtc;
        clocking.ClockOutMethod = request.Method;
        clocking.ClockOutKioskDeviceId = kiosk.Id;
        clocking.ClockOutEvidencePath = NormalizePath(request.EvidencePath);
        clocking.Status = AttendanceStatus.Completed;
        clocking.WorkedMinutes = Math.Max(0,
            (int)Math.Floor((nowUtc - clocking.ClockInTimeUtc).TotalMinutes) - clocking.BreakMinutes);
        clocking.OvertimeMinutes = CalculateOvertime(clocking);

        await _context.SaveChangesAsync(cancellationToken);
        return Map(employee, clocking);
    }

    public async Task<AttendanceStatusDto> Handle(GetTodayAttendanceStatusQuery request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(
            x => x.Id == request.EmployeeId && x.BranchId == request.BranchId && x.IsActive && !x.IsDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Empleado no encontrado en la sucursal del kiosco.");
        var timeZoneId = await GetBranchTimeZoneId(request.BranchId, cancellationToken);
        var workDate = BranchTimeZone.DateFromUtc(DateTime.UtcNow, timeZoneId);
        var clocking = await _context.EmployeeClockings.Include(x => x.Break).FirstOrDefaultAsync(
            x => x.EmployeeId == employee.Id && x.WorkDate == workDate && !x.IsDeleted,
            cancellationToken);
        return clocking is null
            ? new AttendanceStatusDto { EmployeeId = employee.Id, EmployeeName = FullName(employee), WorkDate = workDate }
            : Map(employee, clocking);
    }

    public async Task<List<AttendanceKioskDto>> Handle(GetAttendanceKiosksQuery request, CancellationToken cancellationToken)
    {
        return await _context.AttendanceKioskDevices
            .Where(x => x.BranchId == request.BranchId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new AttendanceKioskDto
            {
                Id = x.Id,
                Name = x.Name,
                DeviceIdentifier = x.DeviceIdentifier,
                Status = x.Status.ToString(),
                ActivatedAtUtc = x.ActivatedAtUtc,
                LastSeenAtUtc = x.LastSeenAtUtc,
                AppVersion = x.AppVersion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<KioskRegistrationDto> Handle(RegisterKioskCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Branches.AnyAsync(x => x.Id == request.BranchId && x.IsActive && !x.IsDeleted, cancellationToken))
            throw new KeyNotFoundException("Sucursal no encontrada.");
        if (await _context.AttendanceKioskDevices.AnyAsync(x => x.DeviceIdentifier == request.DeviceIdentifier && !x.IsDeleted, cancellationToken))
            throw new InvalidOperationException("El dispositivo ya está registrado.");

        var apiKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var kiosk = new AttendanceKioskDevice
        {
            BranchId = request.BranchId,
            Name = request.Name.Trim(),
            DeviceIdentifier = request.DeviceIdentifier.Trim(),
            ApiKeyHash = _passwordHashing.HashPassword(apiKey),
            Status = KioskDeviceStatus.Active,
            ActivatedAtUtc = DateTime.UtcNow
        };
        _context.AttendanceKioskDevices.Add(kiosk);
        await _context.SaveChangesAsync(cancellationToken);
        return new KioskRegistrationDto { KioskId = kiosk.Id, Name = kiosk.Name, DeviceIdentifier = kiosk.DeviceIdentifier, ApiKey = apiKey };
    }

    public async Task<bool> Handle(RevokeKioskCommand request, CancellationToken cancellationToken)
    {
        var kiosk = await _context.AttendanceKioskDevices.FirstOrDefaultAsync(
            x => x.Id == request.KioskId && x.BranchId == request.BranchId && !x.IsDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Kiosco no encontrado.");
        kiosk.Status = KioskDeviceStatus.Revoked;
        kiosk.RevokedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FacialEnrollmentDto> Handle(EnrollEmployeeFaceCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(
            x => x.Id == request.EmployeeId && x.BranchId == request.BranchId && x.IsActive && !x.IsDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Empleado activo no encontrado.");

        var embeddings = new List<float[]>(request.Samples.Count);
        for (var sampleIndex = 0; sampleIndex < request.Samples.Count; sampleIndex++)
        {
            FaceEmbeddingResult result;
            try
            {
                result = await _biometricService.ExtractEmbeddingAsync(request.Samples[sampleIndex], cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Muestra {sampleIndex + 1}: {ex.Message}", ex);
            }
            if (result.FaceWidthRatio < EnrollmentMinimumFaceWidthRatio ||
                result.FaceHeightRatio < EnrollmentMinimumFaceHeightRatio)
                throw new InvalidOperationException(
                    $"Muestra {sampleIndex + 1}: el rostro está demasiado lejos. Acércate hasta ocupar el óvalo.");
            if (result.HorizontalCenterOffset > EnrollmentMaximumHorizontalOffset ||
                result.VerticalCenterOffset > EnrollmentMaximumVerticalOffset)
                throw new InvalidOperationException(
                    $"Muestra {sampleIndex + 1}: el rostro debe estar centrado dentro del óvalo.");
            embeddings.Add(result.Embedding);
        }

        for (var first = 0; first < embeddings.Count; first++)
        for (var second = first + 1; second < embeddings.Count; second++)
            if (SFaceBiometricService.CosineSimilarity(embeddings[first], embeddings[second]) <
                EnrollmentSampleSimilarityThreshold)
                throw new InvalidOperationException(
                    "Las tres muestras deben corresponder al mismo rostro. Repite el enrolamiento.");

        var averaged = AverageAndNormalize(embeddings);
        var encrypted = _facialTemplateProtector.Protect(SerializeEmbedding(averaged));
        var template = await _context.EmployeeFacialTemplates.FirstOrDefaultAsync(
            x => x.EmployeeId == employee.Id && x.ModelVersion == SFaceBiometricService.ModelVersion && !x.IsDeleted,
            cancellationToken);

        if (template is null)
        {
            template = new EmployeeFacialTemplate
            {
                BranchId = request.BranchId,
                EmployeeId = employee.Id,
                ModelVersion = SFaceBiometricService.ModelVersion
            };
            _context.EmployeeFacialTemplates.Add(template);
        }

        template.EncryptedEmbedding = encrypted;
        template.EmbeddingDimensions = averaged.Length;
        template.SampleCount = request.Samples.Count;
        template.EnrolledAtUtc = DateTime.UtcNow;
        template.EnrolledByUserId = request.EnrolledByUserId;
        template.RevokedAtUtc = null;
        await _context.SaveChangesAsync(cancellationToken);

        return new FacialEnrollmentDto
        {
            EmployeeId = employee.Id,
            EmployeeName = FullName(employee),
            ModelVersion = template.ModelVersion,
            SampleCount = template.SampleCount,
            EnrolledAtUtc = template.EnrolledAtUtc
        };
    }

    public async Task<bool> Handle(RevokeEmployeeFaceCommand request, CancellationToken cancellationToken)
    {
        var templates = await _context.EmployeeFacialTemplates.Where(x =>
                x.EmployeeId == request.EmployeeId && x.BranchId == request.BranchId &&
                x.RevokedAtUtc == null && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        if (templates.Count == 0) throw new KeyNotFoundException("El empleado no tiene biometría facial activa.");
        var nowUtc = DateTime.UtcNow;
        foreach (var template in templates) template.RevokedAtUtc = nowUtc;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FaceIdentificationDto> Handle(IdentifyEmployeeFaceQuery request,
        CancellationToken cancellationToken)
    {
        var probe = await _biometricService.ExtractEmbeddingAsync(request.Image, cancellationToken);
        var candidates = await _context.EmployeeFacialTemplates
            .Include(x => x.Employee)
            .Where(x => x.BranchId == request.BranchId && x.ModelVersion == SFaceBiometricService.ModelVersion &&
                        x.RevokedAtUtc == null && !x.IsDeleted && x.Employee != null &&
                        x.Employee.IsActive && !x.Employee.IsDeleted)
            .ToListAsync(cancellationToken);
        if (candidates.Count == 0) throw new KeyNotFoundException("No existen empleados enrolados en esta sucursal.");

        var scores = candidates.Select(template => new
            {
                Template = template,
                Score = SFaceBiometricService.CosineSimilarity(probe.Embedding,
                    DeserializeEmbedding(_facialTemplateProtector.Unprotect(template.EncryptedEmbedding)))
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var best = scores[0];
        if (best.Score < FaceSimilarityThreshold)
            throw new KeyNotFoundException("Rostro no reconocido.");
        if (scores.Count > 1 && best.Score - scores[1].Score < FaceAmbiguityMargin)
            throw new InvalidOperationException("Coincidencia facial ambigua. Solicite asistencia administrativa.");

        return new FaceIdentificationDto
        {
            EmployeeId = best.Template.EmployeeId,
            EmployeeName = FullName(best.Template.Employee!),
            Similarity = Math.Round(best.Score, 4)
        };
    }

    public async Task<List<FacialEnrollmentDto>> Handle(GetFacialEnrollmentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.EmployeeFacialTemplates
            .Where(x => x.BranchId == request.BranchId && x.RevokedAtUtc == null && !x.IsDeleted)
            .OrderBy(x => x.Employee!.LastName).ThenBy(x => x.Employee!.FirstName)
            .Select(x => new FacialEnrollmentDto
            {
                EmployeeId = x.EmployeeId,
                EmployeeName = (x.Employee!.FirstName + " " + x.Employee.LastName).Trim(),
                ModelVersion = x.ModelVersion,
                SampleCount = x.SampleCount,
                EnrolledAtUtc = x.EnrolledAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AttendanceAdminRowDto>> Handle(GetAttendanceAdminRowsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.EmployeeClockings
            .Where(x => x.BranchId == request.BranchId && x.WorkDate >= request.FromDate &&
                        x.WorkDate <= request.ToDate && !x.IsDeleted);
        if (request.EmployeeId.HasValue) query = query.Where(x => x.EmployeeId == request.EmployeeId.Value);

        return await query.OrderByDescending(x => x.WorkDate).ThenBy(x => x.Employee!.LastName)
            .Select(x => new AttendanceAdminRowDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = (x.Employee!.FirstName + " " + x.Employee.LastName).Trim(),
                WorkDate = x.WorkDate,
                Status = x.Status,
                ClockInTimeUtc = x.ClockInTimeUtc,
                ClockOutTimeUtc = x.ClockOutTimeUtc,
                BreakStartedAtUtc = x.Break != null ? x.Break.StartedAtUtc : null,
                BreakEndedAtUtc = x.Break != null ? x.Break.EndedAtUtc : null,
                BreakMinutes = x.BreakMinutes,
                LateMinutes = x.LateMinutes,
                EarlyArrivalMinutes = x.EarlyArrivalMinutes,
                WorkedMinutes = x.WorkedMinutes,
                OvertimeMinutes = x.OvertimeMinutes,
                AdministrativeNotes = x.AdministrativeNotes,
                CorrectionCount = _context.AttendanceCorrections.Count(c =>
                    c.EmployeeClockingId == x.Id && !c.IsDeleted)
            }).ToListAsync(cancellationToken);
    }

    public async Task<List<AttendanceCorrectionDto>> Handle(GetAttendanceCorrectionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!await _context.EmployeeClockings.AnyAsync(x => x.Id == request.EmployeeClockingId &&
                x.BranchId == request.BranchId && !x.IsDeleted, cancellationToken))
            throw new KeyNotFoundException("Jornada no encontrada.");
        return await _context.AttendanceCorrections
            .Where(x => x.EmployeeClockingId == request.EmployeeClockingId && !x.IsDeleted)
            .OrderByDescending(x => x.CorrectedAtUtc)
            .Select(x => new AttendanceCorrectionDto
            {
                Id = x.Id,
                CorrectedByUserId = x.CorrectedByUserId,
                CorrectedAtUtc = x.CorrectedAtUtc,
                Reason = x.Reason,
                BeforeJson = x.BeforeJson,
                AfterJson = x.AfterJson
            }).ToListAsync(cancellationToken);
    }

    public async Task<AttendanceAdminRowDto> Handle(CorrectAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var clocking = await _context.EmployeeClockings.Include(x => x.Employee).Include(x => x.Break)
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeClockingId && x.BranchId == request.BranchId &&
                                      !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Jornada no encontrada.");
        var beforeJson = SerializeClockingSnapshot(clocking);
        var clockIn = AsUtc(request.ClockInTimeUtc);
        DateTime? clockOut = request.ClockOutTimeUtc.HasValue ? AsUtc(request.ClockOutTimeUtc.Value) : null;
        DateTime? breakStart = request.BreakStartedAtUtc.HasValue ? AsUtc(request.BreakStartedAtUtc.Value) : null;
        DateTime? breakEnd = request.BreakEndedAtUtc.HasValue ? AsUtc(request.BreakEndedAtUtc.Value) : null;

        if (clockOut.HasValue && clockOut <= clockIn)
            throw new InvalidOperationException("La salida debe ser posterior a la entrada.");
        if (breakStart.HasValue && (breakStart < clockIn || clockOut.HasValue && breakStart > clockOut))
            throw new InvalidOperationException("El descanso debe estar dentro de la jornada.");
        if (breakEnd.HasValue && (!breakStart.HasValue || breakEnd <= breakStart || clockOut.HasValue && breakEnd > clockOut))
            throw new InvalidOperationException("El fin del descanso no es válido.");
        if (clockOut.HasValue && breakStart.HasValue && !breakEnd.HasValue) breakEnd = clockOut;

        clocking.ClockInTimeUtc = clockIn;
        clocking.ClockOutTimeUtc = clockOut;
        clocking.ClockInMethod = AttendanceMethod.Manual;
        clocking.ClockOutMethod = clockOut.HasValue ? AttendanceMethod.Manual : null;
        clocking.AdministrativeNotes = request.Reason.Trim();

        if (!breakStart.HasValue && clocking.Break is not null)
        {
            var existingBreak = clocking.Break;
            _context.EmployeeClockingBreaks.Remove(existingBreak);
            clocking.Break = null;
        }
        else if (breakStart.HasValue)
        {
            if (clocking.Break is null)
            {
                clocking.Break = new EmployeeClockingBreak
                {
                    BranchId = request.BranchId,
                    EmployeeClockingId = clocking.Id
                };
                _context.EmployeeClockingBreaks.Add(clocking.Break);
            }
            clocking.Break.StartedAtUtc = breakStart.Value;
            clocking.Break.EndedAtUtc = breakEnd;
            clocking.Break.StartMethod = AttendanceMethod.Manual;
            clocking.Break.EndMethod = breakEnd.HasValue ? AttendanceMethod.Manual : null;
            clocking.Break.ClosedAutomaticallyOnClockOut = clockOut.HasValue && breakEnd == clockOut;
            clocking.Break.DurationMinutes = breakEnd.HasValue
                ? Math.Max(0, (int)Math.Ceiling((breakEnd.Value - breakStart.Value).TotalMinutes)) : 0;
        }

        var timeZoneId = await GetBranchTimeZoneId(request.BranchId, cancellationToken);
        Recalculate(clocking, timeZoneId);
        var afterJson = SerializeClockingSnapshot(clocking);
        _context.AttendanceCorrections.Add(new AttendanceCorrection
        {
            BranchId = request.BranchId,
            EmployeeClockingId = clocking.Id,
            CorrectedByUserId = request.CorrectedByUserId,
            CorrectedAtUtc = DateTime.UtcNow,
            Reason = request.Reason.Trim(),
            BeforeJson = beforeJson,
            AfterJson = afterJson
        });
        await _context.SaveChangesAsync(cancellationToken);
        return MapAdmin(clocking, clocking.Employee!, await _context.AttendanceCorrections.CountAsync(
            x => x.EmployeeClockingId == clocking.Id && !x.IsDeleted, cancellationToken));
    }

    private async Task<(Employee Employee, AttendanceKioskDevice Kiosk)> LoadEmployeeAndKiosk(
        KioskAttendanceCommand request, CancellationToken cancellationToken)
    {
        var kiosk = await _context.AttendanceKioskDevices.FirstOrDefaultAsync(
            x => x.Id == request.KioskDeviceId && x.Status == KioskDeviceStatus.Active && !x.IsDeleted,
            cancellationToken) ?? throw new UnauthorizedAccessException("Kiosco no autorizado.");
        var employee = await _context.Employees.FirstOrDefaultAsync(
            x => x.Id == request.EmployeeId && x.BranchId == kiosk.BranchId && x.IsActive && !x.IsDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Empleado no encontrado en la sucursal del kiosco.");
        return (employee, kiosk);
    }

    private async Task<EmployeeClocking> LoadTodayClocking(Guid employeeId, Guid branchId, bool includeBreak, CancellationToken cancellationToken)
    {
        var query = _context.EmployeeClockings.AsQueryable();
        if (includeBreak) query = query.Include(x => x.Break);
        var timeZoneId = await GetBranchTimeZoneId(branchId, cancellationToken);
        var workDate = BranchTimeZone.DateFromUtc(DateTime.UtcNow, timeZoneId);
        return await query.FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.WorkDate == workDate && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("El empleado no tiene una jornada registrada para hoy.");
    }

    private async Task<string?> GetBranchTimeZoneId(Guid branchId, CancellationToken cancellationToken)
    {
        return await _context.Branches
            .AsNoTracking()
            .Where(x => x.Id == branchId && !x.IsDeleted)
            .Select(x => x.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void CloseBreak(EmployeeClocking clocking, DateTime nowUtc, AttendanceMethod method,
        Guid kioskId, string? evidencePath, bool automatic)
    {
        var item = clocking.Break!;
        item.EndedAtUtc = nowUtc;
        item.EndMethod = method;
        item.EndKioskDeviceId = kioskId;
        item.EndEvidencePath = NormalizePath(evidencePath);
        item.DurationMinutes = Math.Max(0, (int)Math.Ceiling((nowUtc - item.StartedAtUtc).TotalMinutes));
        item.ClosedAutomaticallyOnClockOut = automatic;
        clocking.BreakMinutes = item.DurationMinutes;
        clocking.Status = AttendanceStatus.Working;
    }

    private static int CalculateOvertime(EmployeeClocking clocking)
    {
        if (!clocking.ScheduledStartTime.HasValue || !clocking.ScheduledEndTime.HasValue) return 0;
        var scheduled = clocking.ScheduledEndTime.Value - clocking.ScheduledStartTime.Value;
        if (scheduled < TimeSpan.Zero) scheduled += TimeSpan.FromDays(1);
        var scheduledNetMinutes = Math.Max(0, (int)scheduled.TotalMinutes - BreakLimitMinutes);
        return Math.Max(0, clocking.WorkedMinutes - scheduledNetMinutes);
    }

    private static AttendanceStatusDto Map(Employee employee, EmployeeClocking clocking) => new()
    {
        EmployeeId = employee.Id,
        EmployeeName = FullName(employee),
        WorkDate = clocking.WorkDate,
        Status = clocking.Status,
        ClockInTimeUtc = clocking.ClockInTimeUtc,
        ClockOutTimeUtc = clocking.ClockOutTimeUtc,
        BreakStartedAtUtc = clocking.Break?.StartedAtUtc,
        BreakEndedAtUtc = clocking.Break?.EndedAtUtc,
        BreakMinutes = clocking.Break?.EndedAtUtc is null && clocking.Break is not null
            ? Math.Max(0, (int)Math.Ceiling((DateTime.UtcNow - clocking.Break.StartedAtUtc).TotalMinutes))
            : clocking.BreakMinutes,
        LateMinutes = clocking.LateMinutes,
        EarlyArrivalMinutes = clocking.EarlyArrivalMinutes,
        WorkedMinutes = clocking.WorkedMinutes,
        OvertimeMinutes = clocking.OvertimeMinutes
    };

    private static float[] AverageAndNormalize(IReadOnlyList<float[]> embeddings)
    {
        if (embeddings.Count == 0) throw new ArgumentException("No existen muestras faciales.");
        var dimensions = embeddings[0].Length;
        if (dimensions != SFaceBiometricService.EmbeddingDimensions || embeddings.Any(x => x.Length != dimensions))
            throw new InvalidOperationException("Las muestras faciales tienen dimensiones incompatibles.");

        var average = new float[dimensions];
        foreach (var embedding in embeddings)
            for (var index = 0; index < dimensions; index++) average[index] += embedding[index];

        double squaredLength = 0;
        for (var index = 0; index < average.Length; index++)
        {
            average[index] /= embeddings.Count;
            squaredLength += average[index] * average[index];
        }

        var length = Math.Sqrt(squaredLength);
        if (length <= double.Epsilon) throw new InvalidOperationException("No se pudo consolidar la plantilla facial.");
        for (var index = 0; index < average.Length; index++) average[index] = (float)(average[index] / length);
        return average;
    }

    private static string SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    private static float[] DeserializeEmbedding(string serialized)
    {
        var bytes = Convert.FromBase64String(serialized);
        if (bytes.Length != SFaceBiometricService.EmbeddingDimensions * sizeof(float))
            throw new InvalidOperationException("La plantilla facial almacenada no es compatible con SFace.");
        var embedding = new float[SFaceBiometricService.EmbeddingDimensions];
        Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
        return embedding;
    }

    private static void Recalculate(EmployeeClocking clocking, string? timeZoneId)
    {
        var localClockIn = BranchTimeZone.FromUtc(clocking.ClockInTimeUtc, timeZoneId);
        var delta = clocking.ScheduledStartTime.HasValue
            ? (int)Math.Round((localClockIn.TimeOfDay - clocking.ScheduledStartTime.Value).TotalMinutes) : 0;
        clocking.LateMinutes = Math.Max(0, delta);
        clocking.EarlyArrivalMinutes = Math.Max(0, -delta);
        clocking.BreakMinutes = clocking.Break?.DurationMinutes ?? 0;
        clocking.WorkedMinutes = clocking.ClockOutTimeUtc.HasValue
            ? Math.Max(0, (int)Math.Floor((clocking.ClockOutTimeUtc.Value - clocking.ClockInTimeUtc).TotalMinutes) -
                          clocking.BreakMinutes) : 0;
        clocking.OvertimeMinutes = clocking.ClockOutTimeUtc.HasValue ? CalculateOvertime(clocking) : 0;
        clocking.Status = clocking.ClockOutTimeUtc.HasValue
            ? AttendanceStatus.Completed
            : clocking.Break is { EndedAtUtc: null } ? AttendanceStatus.OnBreak : AttendanceStatus.Working;
    }

    private static string SerializeClockingSnapshot(EmployeeClocking clocking) => JsonSerializer.Serialize(new
    {
        clocking.ClockInTimeUtc,
        clocking.ClockOutTimeUtc,
        BreakStartedAtUtc = clocking.Break?.StartedAtUtc,
        BreakEndedAtUtc = clocking.Break?.EndedAtUtc,
        clocking.Status,
        clocking.LateMinutes,
        clocking.EarlyArrivalMinutes,
        clocking.BreakMinutes,
        clocking.WorkedMinutes,
        clocking.OvertimeMinutes,
        clocking.AdministrativeNotes
    });

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static AttendanceAdminRowDto MapAdmin(EmployeeClocking item, Employee employee, int correctionCount) => new()
    {
        Id = item.Id,
        EmployeeId = item.EmployeeId,
        EmployeeName = FullName(employee),
        WorkDate = item.WorkDate,
        Status = item.Status,
        ClockInTimeUtc = item.ClockInTimeUtc,
        ClockOutTimeUtc = item.ClockOutTimeUtc,
        BreakStartedAtUtc = item.Break?.StartedAtUtc,
        BreakEndedAtUtc = item.Break?.EndedAtUtc,
        BreakMinutes = item.BreakMinutes,
        LateMinutes = item.LateMinutes,
        EarlyArrivalMinutes = item.EarlyArrivalMinutes,
        WorkedMinutes = item.WorkedMinutes,
        OvertimeMinutes = item.OvertimeMinutes,
        AdministrativeNotes = item.AdministrativeNotes,
        CorrectionCount = correctionCount
    };

    private static string FullName(Employee employee) => $"{employee.FirstName} {employee.LastName}".Trim();
    private static string? NormalizePath(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
