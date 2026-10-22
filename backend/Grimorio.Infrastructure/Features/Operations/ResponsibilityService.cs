using System.Text.Json;
using Grimorio.Application.Abstractions;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Operations;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Operations;

public class ResponsibilityService(GrimorioDbContext db, ICurrentUserContext user)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Json)!;
    private static string Write<T>(T value) => JsonSerializer.Serialize(value, Json);

    private async Task Lock(Guid branch, CancellationToken ct) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({$"responsibilities:{branch}"}))", ct);

    private async Task<Grimorio.Domain.Entities.Organization.Employee> Employee(Guid branch, CancellationToken ct) =>
        await db.Users.Where(u => u.Id == user.UserId && u.BranchId == branch && u.IsActive && !u.IsDeleted)
            .Select(u => u.Employee).FirstOrDefaultAsync(ct) is { IsActive: true } employee && employee.BranchId == branch && !employee.IsDeleted
            ? employee : throw new InvalidOperationException("Tu usuario debe estar vinculado a un empleado activo de esta sucursal.");

    public async Task<ResponsibilityCatalogDto> Catalog(Guid branch, CancellationToken ct)
    {
        var tasks = await db.ResponsibilityTasks.Where(x => x.BranchId == branch).OrderBy(x => x.Name).ToListAsync(ct);
        var assignments = await db.ResponsibilityAssignments.Where(x => x.BranchId == branch).OrderByDescending(x => x.FromDate).Take(500).ToListAsync(ct);
        var users = await db.Users.Where(x => x.BranchId == branch && x.IsActive && x.EmployeeId != null).Select(x => x.EmployeeId!.Value).ToListAsync(ct);
        var employees = await db.Employees.Where(x => x.BranchId == branch && x.IsActive).OrderBy(x => x.FirstName).ToListAsync(ct);
        return new()
        {
            Areas = await db.ResponsibilityAreas.Where(x => x.BranchId == branch).OrderBy(x => x.Name).Select(x => new ResponsibilityNamedDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct),
            Positions = await db.ResponsibilityPositions.Where(x => x.BranchId == branch).OrderBy(x => x.Name).Select(x => new ResponsibilityNamedDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct),
            Employees = employees.Select(x => new ResponsibilityEmployeeDto(x.Id, $"{x.FirstName} {x.LastName}".Trim(), users.Contains(x.Id))).ToList(),
            Tasks = tasks.Select(MapTask).ToList(), Assignments = assignments.Select(MapAssignment).ToList(),
        };
    }

    public async Task<ResponsibilityNamedDto> SaveNamed(Guid branch, string kind, ResponsibilityNamedDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 120) throw new InvalidOperationException("Escribe un nombre de hasta 120 caracteres.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var name = dto.Name.Trim();
        if (kind == "areas")
        {
            if (await db.ResponsibilityAreas.AnyAsync(x => x.BranchId == branch && x.Id != dto.Id && x.Name.ToLower() == name.ToLower(), ct)) throw new InvalidOperationException("Ya existe un area con ese nombre.");
            var area = dto.Id == Guid.Empty ? new ResponsibilityArea { BranchId = branch } : await db.ResponsibilityAreas.SingleOrDefaultAsync(x => x.Id == dto.Id && x.BranchId == branch, ct) ?? throw new InvalidOperationException("Area no encontrada.");
            if (dto.Id == Guid.Empty) db.ResponsibilityAreas.Add(area);
            area.Name = name; area.IsActive = dto.IsActive; dto.Id = area.Id;
        }
        else if (kind == "positions")
        {
            if (await db.ResponsibilityPositions.AnyAsync(x => x.BranchId == branch && x.Id != dto.Id && x.Name.ToLower() == name.ToLower(), ct)) throw new InvalidOperationException("Ya existe un puesto con ese nombre.");
            var position = dto.Id == Guid.Empty ? new ResponsibilityPosition { BranchId = branch } : await db.ResponsibilityPositions.SingleOrDefaultAsync(x => x.Id == dto.Id && x.BranchId == branch, ct) ?? throw new InvalidOperationException("Puesto no encontrado.");
            if (dto.Id == Guid.Empty) db.ResponsibilityPositions.Add(position);
            position.Name = name; position.IsActive = dto.IsActive; dto.Id = position.Id;
        }
        else throw new InvalidOperationException("Catalogo no valido.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); dto.Name = name; return dto;
    }

    public async Task<ResponsibilityTaskDto> SaveTask(Guid branch, ResponsibilityTaskDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<ResponsibilityFrequency>(dto.Frequency, out var frequency) || !Enum.IsDefined(frequency)
            || !Enum.TryParse<ResponsibilityStage>(dto.Stage, out var stage) || !Enum.IsDefined(stage)
            || !Enum.TryParse<ResponsibilityPhotoMode>(dto.PhotoMode, out var photo) || !Enum.IsDefined(photo)) throw new InvalidOperationException("Frecuencia, momento o evidencia no validos.");
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 160 || dto.Instructions.Length > 2000 || dto.AnchorDate.Year < 2020
            || dto.DaysOfWeekMask is < 1 or > 127 || dto.EveryNDays is < 1 or > 366 || dto.WindowMinutes is < 5 or > 240
            || (dto.IntervalMinutes.HasValue && (stage != ResponsibilityStage.DuringShift || dto.IntervalMinutes is < 15 or > 720))) throw new InvalidOperationException("Revisa los datos y la programacion de la tarea.");
        if (dto.Checklist.Count > 30 || dto.Checklist.Select(x => x.Key).Distinct().Count() != dto.Checklist.Count
            || dto.Checklist.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 50 || string.IsNullOrWhiteSpace(x.Label) || x.Label.Length > 150
                || x.Options.Count is < 2 or > 10 || x.Options.Distinct().Count() != x.Options.Count || x.Options.Any(o => string.IsNullOrWhiteSpace(o) || o.Length > 100))) throw new InvalidOperationException("Cada control requiere una clave distinta, nombre y entre 2 y 10 opciones.");
        if (!await db.ResponsibilityAreas.AnyAsync(x => x.Id == dto.AreaId && x.BranchId == branch && x.IsActive, ct)
            || !await db.ResponsibilityPositions.AnyAsync(x => x.Id == dto.PositionId && x.BranchId == branch && x.IsActive, ct)) throw new InvalidOperationException("Selecciona un area y puesto activos de esta sucursal.");
        var task = dto.Id == Guid.Empty ? new ResponsibilityTask { BranchId = branch } : await db.ResponsibilityTasks.SingleOrDefaultAsync(x => x.Id == dto.Id && x.BranchId == branch, ct) ?? throw new InvalidOperationException("Tarea no encontrada.");
        if (dto.Checklist.Any(x => x.AttentionOptions.Any(option => !x.Options.Contains(option)))) throw new InvalidOperationException("Las respuestas que requieren novedad deben pertenecer a las opciones del control.");
        if (dto.Id == Guid.Empty) db.ResponsibilityTasks.Add(task);
        task.Name = dto.Name.Trim(); task.Instructions = dto.Instructions.Trim(); task.AreaId = dto.AreaId; task.PositionId = dto.PositionId;
        task.Frequency = frequency; task.Stage = stage; task.PhotoMode = photo; task.AnchorDate = dto.AnchorDate; task.DaysOfWeekMask = dto.DaysOfWeekMask;
        task.EveryNDays = dto.EveryNDays; task.IntervalMinutes = dto.IntervalMinutes; task.WindowMinutes = dto.WindowMinutes; task.ChecklistJson = Write(dto.Checklist); task.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct); return MapTask(task);
    }

    public async Task<ResponsibilityAssignmentDto> SaveAssignment(Guid branch, ResponsibilityAssignmentDto dto, CancellationToken ct)
    {
        if (dto.FromDate.Year < 2020 || dto.ToDate < dto.FromDate || dto.ToDate.DayNumber - dto.FromDate.DayNumber > 366
            || dto.ShiftStartTime < TimeSpan.Zero || dto.ShiftStartTime >= TimeSpan.FromDays(1)) throw new InvalidOperationException("Revisa las fechas y la hora del turno.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        if (!await db.ResponsibilityPositions.AnyAsync(x => x.Id == dto.PositionId && x.BranchId == branch && x.IsActive, ct)
            || !await db.Employees.AnyAsync(x => x.Id == dto.EmployeeId && x.BranchId == branch && x.IsActive, ct)) throw new InvalidOperationException("El puesto y empleado deben estar activos en esta sucursal.");
        if (dto.IsActive && await db.ResponsibilityAssignments.AnyAsync(x => x.BranchId == branch && x.Id != dto.Id && x.IsActive
            && x.FromDate <= dto.ToDate && x.ToDate >= dto.FromDate && x.ShiftStartTime == dto.ShiftStartTime
            && (x.PositionId == dto.PositionId || x.EmployeeId == dto.EmployeeId), ct)) throw new InvalidOperationException("Ese puesto o empleado ya tiene una asignacion en ese periodo y turno. Edita la asignacion existente.");
        var assignment = dto.Id == Guid.Empty ? new ResponsibilityAssignment { BranchId = branch } : await db.ResponsibilityAssignments.SingleOrDefaultAsync(x => x.Id == dto.Id && x.BranchId == branch, ct) ?? throw new InvalidOperationException("Asignacion no encontrada.");
        if (dto.Id == Guid.Empty) db.ResponsibilityAssignments.Add(assignment);
        assignment.PositionId = dto.PositionId; assignment.EmployeeId = dto.EmployeeId; assignment.FromDate = dto.FromDate; assignment.ToDate = dto.ToDate; assignment.ShiftStartTime = dto.ShiftStartTime; assignment.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return MapAssignment(assignment);
    }

    public async Task Generate(Guid branch, CancellationToken ct)
    {
        var zone = await db.Branches.Where(x => x.Id == branch).Select(x => x.TimeZoneId).SingleAsync(ct);
        var today = BranchTimeZone.DateFromUtc(DateTime.UtcNow, zone); var yesterday = today.AddDays(-1);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var tasks = await db.ResponsibilityTasks.Include(x => x.Area).Include(x => x.Position).Where(x => x.BranchId == branch && x.IsActive && x.Area!.IsActive && x.Position!.IsActive).ToListAsync(ct);
        var assignments = await db.ResponsibilityAssignments.Where(x => x.BranchId == branch && x.IsActive && x.FromDate <= today && x.ToDate >= yesterday).ToListAsync(ct);
        var dateStart = yesterday.ToDateTime(TimeOnly.MinValue); var dateEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var shifts = await db.ShiftAssignments.Include(x => x.Employee).Where(x => x.BranchId == branch && !x.IsDeleted && x.Date >= dateStart && x.Date < dateEnd && x.Employee!.IsActive).ToListAsync(ct);
        var existing = await db.ResponsibilityActivities.Where(x => x.BranchId == branch && x.WorkDate >= yesterday && x.WorkDate <= today)
            .Select(x => new { x.TaskId, x.ShiftAssignmentId, x.Sequence, x.WorkDate, x.AvailableAtUtc }).ToListAsync(ct);
        var keys = existing.Select(x => (x.TaskId, x.ShiftAssignmentId, x.Sequence)).ToHashSet();
        var times = existing.Select(x => (x.TaskId, x.WorkDate, x.AvailableAtUtc)).ToHashSet();
        foreach (var shift in shifts)
        {
            var date = DateOnly.FromDateTime(shift.Date);
            var start = shift.Date.Date.Add(shift.StartTime); var end = shift.Date.Date.Add(shift.EndTime); if (end <= start) end = end.AddDays(1);
            var utcEnd = BranchTimeZone.ToUtc(end, zone);
            if (date == yesterday && utcEnd < DateTime.UtcNow) continue;
            var assignment = assignments.SingleOrDefault(x => x.EmployeeId == shift.EmployeeId && x.FromDate <= date && x.ToDate >= date && x.ShiftStartTime == shift.StartTime);
            if (assignment == null) continue;
            foreach (var task in tasks.Where(x => x.PositionId == assignment.PositionId && ResponsibilitySchedule.IsDue(x, date)))
            {
                var sequence = 0;
                foreach (var localTime in ResponsibilitySchedule.Times(task, start, end))
                {
                    var index = sequence++;
                    if (!keys.Add((task.Id, shift.Id, index))) continue;
                    var utcTime = BranchTimeZone.ToUtc(localTime, zone);
                    if (!times.Add((task.Id, date, utcTime))) continue;
                    var due = localTime.AddMinutes(task.WindowMinutes); if (due > end) due = end;
                    db.ResponsibilityActivities.Add(new()
                    {
                        BranchId = branch, TaskId = task.Id, AssignmentId = assignment.Id, ShiftAssignmentId = shift.Id, EmployeeId = shift.EmployeeId,
                        EmployeeName = $"{shift.Employee!.FirstName} {shift.Employee.LastName}".Trim(), TaskName = task.Name, AreaName = task.Area!.Name, PositionName = task.Position!.Name,
                        Instructions = task.Instructions, ChecklistJson = task.ChecklistJson, Stage = task.Stage, PhotoMode = task.PhotoMode, WorkDate = date, Sequence = index,
                        AvailableAtUtc = utcTime, DueAtUtc = BranchTimeZone.ToUtc(due, zone), ShiftEndsAtUtc = utcEnd,
                    });
                }
            }
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<ResponsibilityDayDto> Day(Guid branch, DateOnly? requestedDate, bool mine, CancellationToken ct)
    {
        await Generate(branch, ct);
        var zone = await db.Branches.Where(x => x.Id == branch).Select(x => x.TimeZoneId).SingleAsync(ct);
        var today = BranchTimeZone.DateFromUtc(DateTime.UtcNow, zone); var date = requestedDate ?? today;
        if (Math.Abs(date.DayNumber - today.DayNumber) > 366) throw new InvalidOperationException("Consulta fechas dentro del ultimo o proximo ano.");
        var query = db.ResponsibilityActivities.Include(x => x.Evidence).Where(x => x.BranchId == branch && x.WorkDate == date);
        if (mine)
        {
            var employee = await Employee(branch, ct);
            query = db.ResponsibilityActivities.Include(x => x.Evidence).Where(x => x.BranchId == branch && x.EmployeeId == employee.Id
                && (x.WorkDate == date || (!requestedDate.HasValue && x.WorkDate == today.AddDays(-1) && x.ShiftEndsAtUtc >= DateTime.UtcNow)));
        }
        var activities = await query.OrderBy(x => x.AvailableAtUtc).ToListAsync(ct);
        return new() { WorkDate = date, Activities = activities.Select(a => MapActivity(a, !mine)).ToList(),
            Message = activities.Count == 0 ? "No hay actividades asignadas. Revisa con el encargado tu puesto operativo y horario." : null,
            Warnings = mine ? [] : await Warnings(branch, date, ct) };
    }

    private async Task<List<ResponsibilityScheduleWarningDto>> Warnings(Guid branch, DateOnly date, CancellationToken ct)
    {
        var positions = await db.ResponsibilityPositions.Where(x => x.BranchId == branch && x.IsActive).ToListAsync(ct);
        var assignments = await db.ResponsibilityAssignments.Where(x => x.BranchId == branch && x.IsActive && x.FromDate <= date && x.ToDate >= date).ToListAsync(ct);
        var target = date.ToDateTime(TimeOnly.MinValue);
        var shifts = await db.ShiftAssignments.Where(x => x.BranchId == branch && x.Date == target && !x.IsDeleted).ToListAsync(ct);
        var warnings = new List<ResponsibilityScheduleWarningDto>();
        foreach (var position in positions)
        {
            var owners = assignments.Where(x => x.PositionId == position.Id).ToList();
            if (owners.Count == 0) warnings.Add(new(position.Id, position.Name, date, TimeSpan.Zero, "Puesto sin responsable asignada para esta fecha."));
            foreach (var owner in owners.Where(a => !shifts.Any(s => s.EmployeeId == a.EmployeeId && s.StartTime == a.ShiftStartTime)))
                warnings.Add(new(position.Id, position.Name, date, owner.ShiftStartTime, "El empleado no tiene un turno guardado que empiece a esa hora. No se generaron actividades para esa asignacion."));
        }
        return warnings;
    }

    private async Task<ResponsibilityActivity> OwnActivity(Guid branch, Guid id, CancellationToken ct)
    {
        var employee = await Employee(branch, ct);
        return await db.ResponsibilityActivities.Include(x => x.Evidence).SingleOrDefaultAsync(x => x.Id == id && x.BranchId == branch && x.EmployeeId == employee.Id, ct)
            ?? throw new InvalidOperationException("La actividad no esta asignada a tu empleado.");
    }

    public async Task<ResponsibilityActivityDto> MyActivity(Guid branch, Guid id, CancellationToken ct) => MapActivity(await OwnActivity(branch, id, ct), false);

    public async Task CancelCapture(Guid branch, Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var employee = await Employee(branch, ct);
        var evidence = await db.ResponsibilityEvidence.SingleOrDefaultAsync(x => x.Id == id && x.BranchId == branch && x.EmployeeId == employee.Id, ct);
        if (evidence != null && !evidence.ReceivedAtUtc.HasValue) { evidence.IsCancelled = true; evidence.ExpiresAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct);
    }

    public async Task<ResponsibilityCaptureDto> Capture(Guid branch, Guid id, string phase, CancellationToken ct)
    {
        if (phase is not ("Before" or "After")) throw new InvalidOperationException("Momento de captura no valido.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var activity = await OwnActivity(branch, id, ct); EnsureOpen(activity);
        if (activity.PhotoMode == ResponsibilityPhotoMode.None || (phase == "Before" && activity.PhotoMode != ResponsibilityPhotoMode.BeforeAndAfter)) throw new InvalidOperationException("Esta tarea no requiere esa fotografia.");
        if (phase == "After" && activity.PhotoMode == ResponsibilityPhotoMode.BeforeAndAfter && !activity.Evidence.Any(x => x.EmployeeId == activity.EmployeeId && x.Phase == "Before" && x.ReceivedAtUtc.HasValue))
            throw new InvalidOperationException("Registra primero la fotografia de antes.");
        if (phase == "Before" && activity.Evidence.Any(x => x.EmployeeId == activity.EmployeeId && x.Phase == "After" && x.ReceivedAtUtc.HasValue))
            throw new InvalidOperationException("La evidencia de antes debe tomarse antes de la evidencia final.");
        var now = DateTime.UtcNow;
        if (activity.Evidence.Count(x => x.EmployeeId == activity.EmployeeId && x.Phase == phase && !x.IsCancelled && (x.ReceivedAtUtc.HasValue || x.ExpiresAtUtc > now)) >= 3) throw new InvalidOperationException("Ya hay tres capturas registradas o pendientes para este momento de la tarea.");
        var evidence = new ResponsibilityEvidence { BranchId = branch, ActivityId = id, EmployeeId = activity.EmployeeId, Phase = phase, ExpiresAtUtc = now.AddMinutes(30) };
        db.ResponsibilityEvidence.Add(evidence); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(evidence.Id, evidence.CreatedAt, evidence.ExpiresAtUtc);
    }

    public async Task StoreEvidence(Guid branch, Guid id, DateTime captured, string key, string hash, long size, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var employee = await Employee(branch, ct);
        var evidence = await db.ResponsibilityEvidence.Include(x => x.Activity).SingleOrDefaultAsync(x => x.Id == id && x.BranchId == branch && x.EmployeeId == employee.Id, ct)
            ?? throw new InvalidOperationException("La captura no pertenece a tu empleado.");
        var now = DateTime.UtcNow;
        if (evidence.ReceivedAtUtc.HasValue) throw new InvalidOperationException("Esta captura ya fue recibida. Actualiza la actividad.");
        if (evidence.IsCancelled || evidence.ExpiresAtUtc.AddHours(48) < now || captured.Kind != DateTimeKind.Utc || captured < evidence.CreatedAt || captured > evidence.ExpiresAtUtc || captured > now.AddMinutes(1)) throw new InvalidOperationException("La captura caduco o su hora no es valida. Toma una fotografia nueva.");
        EnsureOpen(evidence.Activity!);
        if (evidence.Activity!.EmployeeId != employee.Id) throw new InvalidOperationException("La actividad fue reasignada.");
        evidence.StorageKey = key; evidence.Sha256 = hash; evidence.SizeBytes = size; evidence.CapturedAtUtc = captured; evidence.ReceivedAtUtc = now;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<string?> EvidencePath(Guid branch, Guid id, bool supervise, CancellationToken ct)
    {
        var employeeId = supervise ? (Guid?)null : (await Employee(branch, ct)).Id;
        return await db.ResponsibilityEvidence.Where(x => x.Id == id && x.BranchId == branch && x.ReceivedAtUtc != null
            && (supervise || x.Activity!.EmployeeId == employeeId)).Select(x => x.StorageKey).SingleOrDefaultAsync(ct);
    }

    public async Task<ResponsibilityActivityDto> Complete(Guid branch, Guid id, CompleteResponsibilityDto dto, CancellationToken ct)
    {
        if (dto.IdempotencyKey == Guid.Empty) throw new InvalidOperationException("Falta el identificador del registro.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var activity = await OwnActivity(branch, id, ct);
        if (activity.CompletionKey == dto.IdempotencyKey) { await tx.CommitAsync(ct); return MapActivity(activity, false); }
        EnsureOpen(activity);
        if (dto.Notes?.Length > 2000 || (dto.HasIssue && string.IsNullOrWhiteSpace(dto.Notes))) throw new InvalidOperationException("Describe la novedad encontrada (maximo 2000 caracteres).");
        var checks = Read<List<ResponsibilityCheckDto>>(activity.ChecklistJson);
        if (!dto.HasIssue && checks.Any(c => dto.Answers.Any(a => a.Key == c.Key && c.AttentionOptions.Contains(a.Value))))
            throw new InvalidOperationException("Una respuesta indica un faltante o problema. Registra la actividad con novedad y describe lo encontrado.");
        if (dto.Answers.Count != checks.Count || dto.Answers.Select(x => x.Key).Distinct().Count() != dto.Answers.Count
            || checks.Any(c => !dto.Answers.Any(a => a.Key == c.Key && c.Options.Contains(a.Value)))) throw new InvalidOperationException("Completa todos los controles de la actividad.");
        var evidence = activity.Evidence.Where(x => x.EmployeeId == activity.EmployeeId && x.ReceivedAtUtc.HasValue).ToList();
        if ((activity.PhotoMode != ResponsibilityPhotoMode.None && !evidence.Any(x => x.Phase == "After"))
            || (activity.PhotoMode == ResponsibilityPhotoMode.BeforeAndAfter && !evidence.Any(x => x.Phase == "Before"))) throw new InvalidOperationException("Faltan las fotografias requeridas de antes o despues.");
        activity.AnswersJson = Write(dto.Answers); activity.Notes = dto.Notes?.Trim(); activity.CompletedAtUtc = DateTime.UtcNow; activity.CompletedByUserId = user.UserId; activity.CompletionKey = dto.IdempotencyKey;
        activity.Status = dto.HasIssue ? ResponsibilityStatus.CompletedWithIssue : ResponsibilityStatus.Completed;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return MapActivity(activity, false);
    }

    public async Task Transfer(Guid branch, Guid id, TransferResponsibilityDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 500) throw new InvalidOperationException("Indica el motivo del reemplazo (maximo 500 caracteres).");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var activity = await db.ResponsibilityActivities.Include(x => x.Evidence).SingleOrDefaultAsync(x => x.Id == id && x.BranchId == branch, ct) ?? throw new InvalidOperationException("Actividad no encontrada.");
        if (activity.Status != ResponsibilityStatus.Pending || activity.EmployeeId == dto.EmployeeId) throw new InvalidOperationException("Solo se pueden reasignar pendientes a otra persona.");
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.Id == dto.EmployeeId && x.BranchId == branch && x.IsActive, ct) ?? throw new InvalidOperationException("Empleado no disponible.");
        var date = activity.WorkDate.ToDateTime(TimeOnly.MinValue);
        if (!await db.ShiftAssignments.AnyAsync(x => x.BranchId == branch && x.EmployeeId == employee.Id && x.Date == date && !x.IsDeleted, ct)) throw new InvalidOperationException("La persona de reemplazo debe tener un turno guardado en esta fecha.");
        db.ResponsibilityTransfers.Add(new() { BranchId = branch, ActivityId = id, FromEmployeeId = activity.EmployeeId, ToEmployeeId = employee.Id, Reason = dto.Reason.Trim() });
        foreach (var pending in activity.Evidence.Where(x => x.ReceivedAtUtc == null)) { pending.IsCancelled = true; pending.ExpiresAtUtc = DateTime.UtcNow; }
        activity.EmployeeId = employee.Id; activity.EmployeeName = $"{employee.FirstName} {employee.LastName}".Trim();
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    private static void EnsureOpen(ResponsibilityActivity activity)
    {
        if (activity.Status != ResponsibilityStatus.Pending) throw new InvalidOperationException("Esta actividad ya fue registrada.");
        if (DateTime.UtcNow < activity.AvailableAtUtc) throw new InvalidOperationException("Esta actividad aun no esta disponible. Espera su horario de revision.");
    }

    private static ResponsibilityTaskDto MapTask(ResponsibilityTask t) => new() { Id = t.Id, AreaId = t.AreaId, PositionId = t.PositionId, Name = t.Name, Instructions = t.Instructions,
        Frequency = t.Frequency.ToString(), Stage = t.Stage.ToString(), PhotoMode = t.PhotoMode.ToString(), AnchorDate = t.AnchorDate, DaysOfWeekMask = t.DaysOfWeekMask,
        EveryNDays = t.EveryNDays, IntervalMinutes = t.IntervalMinutes, WindowMinutes = t.WindowMinutes, Checklist = Read<List<ResponsibilityCheckDto>>(t.ChecklistJson), IsActive = t.IsActive };
    private static ResponsibilityAssignmentDto MapAssignment(ResponsibilityAssignment a) => new() { Id = a.Id, PositionId = a.PositionId, EmployeeId = a.EmployeeId, FromDate = a.FromDate, ToDate = a.ToDate, ShiftStartTime = a.ShiftStartTime, IsActive = a.IsActive };
    private static ResponsibilityActivityDto MapActivity(ResponsibilityActivity a, bool includePreviousOwners = true) => new() { Id = a.Id, EmployeeId = a.EmployeeId, EmployeeName = a.EmployeeName, PositionName = a.PositionName, AreaName = a.AreaName,
        TaskName = a.TaskName, Instructions = a.Instructions, Stage = a.Stage.ToString(), PhotoMode = a.PhotoMode.ToString(), Status = a.Status == ResponsibilityStatus.Pending && a.DueAtUtc < DateTime.UtcNow ? "Overdue" : a.Status.ToString(),
        WorkDate = a.WorkDate, AvailableAtUtc = a.AvailableAtUtc, DueAtUtc = a.DueAtUtc, CompletedAtUtc = a.CompletedAtUtc, Notes = a.Notes, Checklist = Read<List<ResponsibilityCheckDto>>(a.ChecklistJson), Answers = Read<List<ResponsibilityAnswerDto>>(a.AnswersJson),
        Evidence = a.Evidence.Where(e => e.ReceivedAtUtc.HasValue && (includePreviousOwners || e.EmployeeId == a.EmployeeId)).Select(e => new ResponsibilityEvidenceDto(e.Id, e.Phase, e.CapturedAtUtc, e.ReceivedAtUtc)).ToList() };
}
