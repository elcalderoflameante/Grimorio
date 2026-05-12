using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class GenerateMonthlyShiftsCommandHandler : IRequestHandler<GenerateMonthlyShiftsCommand, ShiftGenerationResultDto>
{
    private const int MaxConsecutiveWorkDays = 6;

    private readonly GrimorioDbContext _context;

    public GenerateMonthlyShiftsCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftGenerationResultDto> Handle(GenerateMonthlyShiftsCommand request, CancellationToken cancellationToken)
    {
        if (request.Month < 1 || request.Month > 12)
            throw new InvalidOperationException("Mes inválido.");

        if (request.Year < 2000)
            throw new InvalidOperationException("Año inválido.");

        var startDate = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
        var today = DateTime.Today;
        var generationStartDate = startDate;
        var generationEndDate = endDate;
        var isPartialRangeGeneration = request.RangeStartDate.HasValue && request.RangeEndDate.HasValue;

        if (isPartialRangeGeneration)
        {
            generationStartDate = request.RangeStartDate!.Value.Date;
            generationEndDate = request.RangeEndDate!.Value.Date;

            if (generationStartDate < startDate)
                generationStartDate = startDate;

            if (generationEndDate > endDate)
                generationEndDate = endDate;

            if (today.Year == request.Year && today.Month == request.Month)
            {
                var minDate = today.AddDays(1).Date;
                if (generationStartDate < minDate)
                    generationStartDate = minDate;
            }

            if (generationStartDate > generationEndDate)
                throw new InvalidOperationException("No hay días futuros para generar en el rango semanal seleccionado.");
        }
        else if (today.Year == request.Year && today.Month == request.Month)
        {
            generationStartDate = today.AddDays(1);
            if (generationStartDate < startDate)
                generationStartDate = startDate;

            if (generationStartDate > endDate)
                throw new InvalidOperationException("No hay días futuros para generar en este mes.");
        }

        var config = await _context.ScheduleConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.BranchId == request.BranchId && !sc.IsDeleted, cancellationToken);

        var shiftTemplates = await _context.ShiftTemplates
            .Where(st => st.BranchId == request.BranchId && !st.IsDeleted)
            .Include(st => st.WorkArea)
            .Include(st => st.WorkRole)
            .ToListAsync(cancellationToken);

        if (!shiftTemplates.Any())
            throw new InvalidOperationException("No existen plantillas de turno para esta sucursal.");

        var specialDates = await _context.SpecialDates
            .Where(sd => sd.BranchId == request.BranchId && !sd.IsDeleted
                && sd.Date >= startDate && sd.Date <= endDate)
            .Include(sd => sd.Templates.Where(t => !t.IsDeleted))
            .ThenInclude(t => t.WorkArea)
            .ToListAsync(cancellationToken);

        var specialDateDict = specialDates.ToDictionary(sd => sd.Date.Date);

        var specialTemplateRoleIds = specialDates
            .SelectMany(sd => sd.Templates)
            .Select(t => t.WorkRoleId)
            .Distinct()
            .ToList();

        var workRoles = await _context.WorkRoles
            .Where(wr => specialTemplateRoleIds.Contains(wr.Id) && !wr.IsDeleted)
            .ToDictionaryAsync(wr => wr.Id, wr => wr, cancellationToken);

        var employeeWorkRoles = await _context.EmployeeWorkRoles
            .Where(ewr => !ewr.IsDeleted)
            .Include(ewr => ewr.Employee)
            .Include(ewr => ewr.WorkRole)
            .Where(ewr => ewr.Employee != null && ewr.Employee.IsActive && ewr.Employee.BranchId == request.BranchId)
            .ToListAsync(cancellationToken);

        if (!employeeWorkRoles.Any())
            throw new InvalidOperationException("No hay empleados elegibles con roles asignados.");

        var availability = await _context.EmployeeAvailability
            .Where(ea => !ea.IsDeleted && ea.UnavailableDate >= generationStartDate && ea.UnavailableDate <= generationEndDate)
            .ToListAsync(cancellationToken);

        var availabilityByEmployee = availability
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => a.UnavailableDate.Date).ToHashSet()
            );

        var existingAssignments = await _context.ShiftAssignments
            .Include(sa => sa.Employee)
            .Where(sa => sa.Date >= startDate && sa.Date <= endDate && !sa.IsDeleted)
            .Where(sa => sa.BranchId == request.BranchId
                || (sa.BranchId == Guid.Empty && sa.Employee != null && sa.Employee.BranchId == request.BranchId))
            .ToListAsync(cancellationToken);

        var assignmentsToRegenerate = existingAssignments
            .Where(a => a.Date.Date >= generationStartDate.Date && a.Date.Date <= generationEndDate.Date)
            .ToList();

        var assignmentsToKeep = existingAssignments
            .Where(a => a.Date.Date < generationStartDate.Date || a.Date.Date > generationEndDate.Date)
            .ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var assignment in assignmentsToRegenerate)
            assignment.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);

        var assignedDatesByEmployee = new Dictionary<Guid, HashSet<DateTime>>();
        var hoursByEmployee = new Dictionary<Guid, decimal>();
        var weeklyHoursByEmployee = new Dictionary<Guid, Dictionary<int, decimal>>();
        var assignmentsToCreate = new List<ShiftAssignment>();
        var warnings = new List<ShiftGenerationWarningDto>();
        var uncoveredSlots = new List<(DateTime Date, DayOfWeek DayOfWeek, Guid WorkAreaId, Guid WorkRoleId, TimeSpan StartTime, TimeSpan EndTime, TimeSpan? BreakDuration, TimeSpan? LunchDuration, string? Notes, string WorkAreaName, string WorkRoleName, int MissingCount)>();

        if (isPartialRangeGeneration)
        {
            foreach (var assignment in assignmentsToKeep)
            {
                TrackAssignment(
                    assignment.EmployeeId,
                    assignment.Date,
                    assignment.WorkedHours,
                    startDate,
                    assignedDatesByEmployee,
                    hoursByEmployee,
                    weeklyHoursByEmployee);
            }
        }

        var roleCandidates = employeeWorkRoles
            .GroupBy(ewr => ewr.WorkRoleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var roleCountByEmployee = employeeWorkRoles
            .GroupBy(ewr => ewr.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.WorkRoleId).Distinct().Count());

        var schedulableEmployees = employeeWorkRoles
            .Where(ewr => ewr.Employee != null)
            .Select(ewr => ewr.Employee!)
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();

        var roleIdsByEmployee = employeeWorkRoles
            .GroupBy(ewr => ewr.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.WorkRoleId).Distinct().ToHashSet());

        var demandByDateAndRole = BuildDemandByDateAndRole(startDate, endDate, shiftTemplates, specialDates);
        var restPlans = BuildMonthlyRestPlans(
            schedulableEmployees,
            roleIdsByEmployee,
            demandByDateAndRole,
            availabilityByEmployee,
            startDate,
            endDate,
            generationStartDate,
            generationEndDate);

        warnings.AddRange(BuildPreGenerationWarnings(
            generationStartDate,
            generationEndDate,
            daysInMonth,
            shiftTemplates,
            specialDates,
            workRoles,
            roleCandidates,
            availabilityByEmployee,
            assignedDatesByEmployee));

        for (var currentDate = generationStartDate; currentDate <= generationEndDate; currentDate = currentDate.AddDays(1))
        {
            var dayOfWeek = currentDate.DayOfWeek;

            List<(Guid WorkAreaId, Guid WorkRoleId, TimeSpan StartTime, TimeSpan EndTime, TimeSpan? BreakDuration, TimeSpan? LunchDuration, int RequiredCount, string? Notes, WorkArea? WorkArea, WorkRole? WorkRole)> templatesForDay;

            if (specialDateDict.TryGetValue(currentDate.Date, out var specialDate) && specialDate.Templates.Any())
            {
                templatesForDay = specialDate.Templates
                    .Select(t => (
                        WorkAreaId: t.WorkAreaId,
                        WorkRoleId: t.WorkRoleId,
                        StartTime: t.StartTime,
                        EndTime: t.EndTime,
                        BreakDuration: t.BreakDuration,
                        LunchDuration: t.LunchDuration,
                        RequiredCount: t.RequiredCount,
                        Notes: t.Notes,
                        WorkArea: t.WorkArea,
                        WorkRole: workRoles.TryGetValue(t.WorkRoleId, out var wr) ? wr : null
                    ))
                    .ToList();
            }
            else
            {
                templatesForDay = shiftTemplates
                    .Where(t => t.DayOfWeek == dayOfWeek)
                    .Select(t => (
                        WorkAreaId: t.WorkAreaId,
                        WorkRoleId: t.WorkRoleId,
                        StartTime: t.StartTime,
                        EndTime: t.EndTime,
                        BreakDuration: t.BreakDuration,
                        LunchDuration: t.LunchDuration,
                        RequiredCount: t.RequiredCount,
                        Notes: t.Notes,
                        WorkArea: t.WorkArea,
                        WorkRole: t.WorkRole
                    ))
                    .ToList();
            }

            foreach (var template in templatesForDay)
            {
                var assignedForTemplate = 0;

                if (!roleCandidates.TryGetValue(template.WorkRoleId, out var candidatesForRole))
                {
                    uncoveredSlots.Add((
                        Date: currentDate,
                        DayOfWeek: dayOfWeek,
                        WorkAreaId: template.WorkAreaId,
                        WorkRoleId: template.WorkRoleId,
                        StartTime: template.StartTime,
                        EndTime: template.EndTime,
                        BreakDuration: template.BreakDuration,
                        LunchDuration: template.LunchDuration,
                        Notes: template.Notes,
                        WorkAreaName: template.WorkArea?.Name ?? "Desconocida",
                        WorkRoleName: template.WorkRole?.Name ?? "Desconocido",
                        MissingCount: template.RequiredCount));
                    continue;
                }

                for (var i = 0; i < template.RequiredCount; i++)
                {
                    var eligibleCandidates = candidatesForRole
                        .Where(c => c.Employee != null)
                        .Where(c => IsEmployeeAvailable(c.Employee!.Id, currentDate, availabilityByEmployee))
                        .Where(c => !IsEmployeeAlreadyAssigned(c.Employee!.Id, currentDate, assignedDatesByEmployee))
                        .Where(c => !IsPreferredRestDate(c.Employee!.Id, currentDate, restPlans))
                        .Where(c => CanAssignByFreeDays(c, daysInMonth, assignedDatesByEmployee))
                        .Where(c => CanAssignByMaxConsecutiveDays(c.Employee!.Id, currentDate, assignedDatesByEmployee))
                        .Where(c => CanAssignByHours(c.Employee!, currentDate, startDate, template.StartTime, template.EndTime, template.BreakDuration, template.LunchDuration, weeklyHoursByEmployee))
                        .OrderByDescending(c => c.IsPrimary)
                        .ThenBy(c => c.Priority)
                        .ThenBy(c => GetEmployeeRoleCount(c.EmployeeId, roleCountByEmployee))
                        .ThenByDescending(c => GetRemainingDaysToAssign(c, daysInMonth, assignedDatesByEmployee))
                        .ThenBy(c => WillUseWeeklyExtraHours(c.Employee!, currentDate, startDate, template.StartTime, template.EndTime, template.BreakDuration, template.LunchDuration, weeklyHoursByEmployee))
                        .ThenBy(c => GetEmployeeHours(c.Employee!.Id, hoursByEmployee))
                        .ThenBy(c => GetEmployeeAssignedDays(c.Employee!.Id, assignedDatesByEmployee))
                        .ToList();

                    var selected = eligibleCandidates.FirstOrDefault();
                    if (selected == null)
                        continue;

                    var employeeId = selected.Employee!.Id;
                    var workedHours = CalculateWorkedHours(template.StartTime, template.EndTime, template.BreakDuration, template.LunchDuration);

                    assignmentsToCreate.Add(new ShiftAssignment
                    {
                        BranchId = request.BranchId,
                        EmployeeId = employeeId,
                        Date = currentDate,
                        StartTime = template.StartTime,
                        EndTime = template.EndTime,
                        BreakDuration = template.BreakDuration,
                        LunchDuration = template.LunchDuration,
                        WorkAreaId = template.WorkAreaId,
                        WorkRoleId = template.WorkRoleId,
                        WorkedHours = workedHours,
                        Notes = template.Notes,
                        IsApproved = false,
                    });

                    assignedForTemplate++;
                    TrackAssignment(employeeId, currentDate, workedHours, startDate, assignedDatesByEmployee, hoursByEmployee, weeklyHoursByEmployee);
                }

                if (assignedForTemplate < template.RequiredCount)
                {
                    uncoveredSlots.Add((
                        Date: currentDate,
                        DayOfWeek: dayOfWeek,
                        WorkAreaId: template.WorkAreaId,
                        WorkRoleId: template.WorkRoleId,
                        StartTime: template.StartTime,
                        EndTime: template.EndTime,
                        BreakDuration: template.BreakDuration,
                        LunchDuration: template.LunchDuration,
                        Notes: template.Notes,
                        WorkAreaName: template.WorkArea?.Name ?? "Desconocida",
                        WorkRoleName: template.WorkRole?.Name ?? "Desconocido",
                        MissingCount: template.RequiredCount - assignedForTemplate));
                }
            }
        }

        var remainingUncoveredSlots = AutoAssignUncoveredSlots(
            uncoveredSlots,
            request.BranchId,
            roleCandidates,
            roleCountByEmployee,
            availabilityByEmployee,
            assignedDatesByEmployee,
            hoursByEmployee,
            weeklyHoursByEmployee,
            daysInMonth,
            startDate,
            assignmentsToCreate);

        warnings.AddRange(
            remainingUncoveredSlots
                .Where(s => s.MissingCount > 0)
                .Select(s => new ShiftGenerationWarningDto
                {
                    Date = s.Date,
                    DayOfWeek = s.DayOfWeek,
                    WorkAreaName = s.WorkAreaName,
                    WorkRoleName = s.WorkRoleName,
                    RequiredCount = s.MissingCount,
                    AssignedCount = 0,
                    Reason = $"CoberturaFinal: Faltan {s.MissingCount} cupo(s) en el bloque {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm} para este rol/área tras 2 pasadas automáticas. Puede existir al menos un turno de ese rol ya asignado en esa fecha."
                }));

        _context.ShiftAssignments.AddRange(assignmentsToCreate);
        await _context.SaveChangesAsync(cancellationToken);

        var employeesToValidate = employeeWorkRoles
            .Where(ewr => ewr.Employee != null)
            .Select(ewr => ewr.Employee!)
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();

        if (generationStartDate.Date == startDate.Date)
        {
            foreach (var employee in employeesToValidate)
            {
                var assignedDays = GetEmployeeAssignedDays(employee.Id, assignedDatesByEmployee);
                var requiredWorkingDays = daysInMonth - employee.FreeDaysPerMonth;
                var actualFreeDays = daysInMonth - assignedDays;

                if (employee.ContractType != Domain.Enums.ContractType.PartTime && actualFreeDays != employee.FreeDaysPerMonth)
                {
                    warnings.Add(new ShiftGenerationWarningDto
                    {
                        Date = startDate,
                        DayOfWeek = startDate.DayOfWeek,
                        WorkAreaName = "Cuota empleado",
                        WorkRoleName = "(No aplica)",
                        RequiredCount = requiredWorkingDays,
                        AssignedCount = assignedDays,
                        Reason = $"Empleado {employee.FirstName} {employee.LastName} configurado para {employee.FreeDaysPerMonth} días libres pero tiene {actualFreeDays}. Trabajó {assignedDays} de {requiredWorkingDays} días requeridos."
                    });
                }
            }
        }

        warnings.AddRange(BuildRestComplianceWarnings(
            employeesToValidate,
            restPlans,
            assignedDatesByEmployee,
            startDate,
            endDate,
            daysInMonth));

        var employeeNames = employeeWorkRoles
            .Where(ewr => ewr.Employee != null)
            .Select(ewr => ewr.Employee!)
            .GroupBy(e => e.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var assignments = assignmentsToCreate
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(a => new ShiftAssignmentDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeName = employeeNames.TryGetValue(a.EmployeeId, out var emp)
                    ? $"{emp.FirstName} {emp.LastName}"
                    : string.Empty,
                Date = a.Date,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                BreakDuration = a.BreakDuration,
                LunchDuration = a.LunchDuration,
                WorkAreaId = a.WorkAreaId,
                WorkAreaName = shiftTemplates.First(t => t.WorkAreaId == a.WorkAreaId).WorkArea?.Name ?? string.Empty,
                WorkAreaColor = shiftTemplates.First(t => t.WorkAreaId == a.WorkAreaId).WorkArea?.Color ?? "#808080",
                WorkRoleId = a.WorkRoleId,
                WorkRoleName = shiftTemplates.First(t => t.WorkRoleId == a.WorkRoleId).WorkRole?.Name ?? string.Empty,
                WorkedHours = a.WorkedHours,
                Notes = a.Notes,
                IsApproved = a.IsApproved,
                ApprovedBy = a.ApprovedBy,
                ApprovedAt = a.ApprovedAt
            })
            .ToList();

        await transaction.CommitAsync(cancellationToken);

        return new ShiftGenerationResultDto
        {
            Assignments = assignments,
            Warnings = warnings,
            TotalShiftsGenerated = assignments.Count,
            TotalShiftsNotCovered = warnings
                .Where(w => w.WorkAreaName != "Cuota empleado")
                .Where(w => w.WorkAreaName != "Descanso empleado")
                .Where(w => !w.Reason.StartsWith("PreCheck:", StringComparison.OrdinalIgnoreCase))
                .Sum(w => w.RequiredCount - w.AssignedCount)
        };
    }

    private sealed class EmployeeRestPlan
    {
        public Guid EmployeeId { get; init; }
        public HashSet<DateTime> PreferredRestDates { get; init; } = new();
        public Dictionary<int, int> WeeklyRestTargets { get; init; } = new();
    }

    private static bool IsEmployeeAvailable(Guid employeeId, DateTime date, Dictionary<Guid, HashSet<DateTime>> availabilityByEmployee)
    {
        if (!availabilityByEmployee.TryGetValue(employeeId, out var unavailableDates))
            return true;

        return !unavailableDates.Contains(date.Date);
    }

    private static bool IsEmployeeAlreadyAssigned(Guid employeeId, DateTime date, Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        if (!assignedDatesByEmployee.TryGetValue(employeeId, out var dates))
            return false;

        return dates.Contains(date.Date);
    }

    private static bool IsPreferredRestDate(Guid employeeId, DateTime date, Dictionary<Guid, EmployeeRestPlan> restPlans)
        => restPlans.TryGetValue(employeeId, out var plan) && plan.PreferredRestDates.Contains(date.Date);

    private static bool CanAssignByMaxConsecutiveDays(
        Guid employeeId,
        DateTime dateToAssign,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        var dates = assignedDatesByEmployee.TryGetValue(employeeId, out var current)
            ? new HashSet<DateTime>(current)
            : new HashSet<DateTime>();

        dates.Add(dateToAssign.Date);

        var consecutive = 1;
        var cursor = dateToAssign.Date.AddDays(-1);
        while (dates.Contains(cursor))
        {
            consecutive++;
            cursor = cursor.AddDays(-1);
        }

        cursor = dateToAssign.Date.AddDays(1);
        while (dates.Contains(cursor))
        {
            consecutive++;
            cursor = cursor.AddDays(1);
        }

        return consecutive <= MaxConsecutiveWorkDays;
    }

    private static Dictionary<(DateTime Date, Guid WorkRoleId), int> BuildDemandByDateAndRole(
        DateTime monthStart,
        DateTime monthEnd,
        List<ShiftTemplate> shiftTemplates,
        List<SpecialDate> specialDates)
    {
        var specialDateDict = specialDates.ToDictionary(sd => sd.Date.Date);
        var result = new Dictionary<(DateTime Date, Guid WorkRoleId), int>();

        for (var currentDate = monthStart.Date; currentDate <= monthEnd.Date; currentDate = currentDate.AddDays(1))
        {
            var templatesForDay = specialDateDict.TryGetValue(currentDate, out var specialDate) && specialDate.Templates.Any()
                ? specialDate.Templates.Select(t => (t.WorkRoleId, t.RequiredCount))
                : shiftTemplates
                    .Where(t => t.DayOfWeek == currentDate.DayOfWeek)
                    .Select(t => (t.WorkRoleId, t.RequiredCount));

            foreach (var template in templatesForDay)
            {
                var key = (currentDate, template.WorkRoleId);
                result[key] = result.TryGetValue(key, out var current)
                    ? current + template.RequiredCount
                    : template.RequiredCount;
            }
        }

        return result;
    }

    private static Dictionary<Guid, EmployeeRestPlan> BuildMonthlyRestPlans(
        List<Employee> employees,
        Dictionary<Guid, HashSet<Guid>> roleIdsByEmployee,
        Dictionary<(DateTime Date, Guid WorkRoleId), int> demandByDateAndRole,
        Dictionary<Guid, HashSet<DateTime>> availabilityByEmployee,
        DateTime monthStart,
        DateTime monthEnd,
        DateTime generationStartDate,
        DateTime generationEndDate)
    {
        var result = new Dictionary<Guid, EmployeeRestPlan>();

        foreach (var employee in employees.Where(e => e.ContractType != Domain.Enums.ContractType.PartTime))
        {
            var freeDays = Math.Clamp(employee.FreeDaysPerMonth, 0, (monthEnd.Date - monthStart.Date).Days + 1);
            var weeklyTargets = BuildWeeklyRestTargets(freeDays, monthStart, monthEnd);
            var preferredDates = new HashSet<DateTime>();

            foreach (var target in weeklyTargets.Where(t => t.Value > 0))
            {
                var weekDates = GetDatesForWeekIndex(monthStart, monthEnd, target.Key)
                    .Where(d => d >= generationStartDate.Date && d <= generationEndDate.Date)
                    .Where(d => !preferredDates.Contains(d))
                    .ToList();

                var remaining = Math.Min(target.Value, weekDates.Count);
                while (remaining > 0 && weekDates.Count > 0)
                {
                    if (remaining >= 2)
                    {
                        var pair = FindLowestImpactConsecutivePair(
                            weekDates,
                            employee.Id,
                            roleIdsByEmployee,
                            demandByDateAndRole,
                            availabilityByEmployee);

                        if (pair != null)
                        {
                            preferredDates.Add(pair.Value.First);
                            preferredDates.Add(pair.Value.Second);
                            weekDates.Remove(pair.Value.First);
                            weekDates.Remove(pair.Value.Second);
                            remaining -= 2;
                            continue;
                        }
                    }

                    var bestSingle = weekDates
                        .OrderBy(d => GetRestImpactScore(employee.Id, d, roleIdsByEmployee, demandByDateAndRole, availabilityByEmployee))
                        .ThenBy(d => d.Day)
                        .First();

                    preferredDates.Add(bestSingle);
                    weekDates.Remove(bestSingle);
                    remaining--;
                }
            }

            result[employee.Id] = new EmployeeRestPlan
            {
                EmployeeId = employee.Id,
                PreferredRestDates = preferredDates,
                WeeklyRestTargets = weeklyTargets
            };
        }

        return result;
    }

    private static Dictionary<int, int> BuildWeeklyRestTargets(int freeDaysPerMonth, DateTime monthStart, DateTime monthEnd)
    {
        var weekCount = GetWeekIndex(monthEnd, monthStart) + 1;
        var targets = Enumerable.Range(0, weekCount).ToDictionary(i => i, _ => 0);
        var remaining = freeDaysPerMonth;
        var primaryWeekCount = Math.Min(4, weekCount);

        for (var i = 0; i < primaryWeekCount && remaining > 0; i++)
        {
            targets[i]++;
            remaining--;
        }

        var preferredExtraOrder = new[] { 1, 3, 0, 2 };
        foreach (var weekIndex in preferredExtraOrder.Where(i => i < primaryWeekCount))
        {
            if (remaining <= 0)
                break;

            if (targets[weekIndex] >= 2)
                continue;

            targets[weekIndex]++;
            remaining--;
        }

        while (remaining > 0)
        {
            var weekIndex = targets
                .OrderBy(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key)
                .First().Key;

            targets[weekIndex]++;
            remaining--;
        }

        return targets;
    }

    private static List<DateTime> GetDatesForWeekIndex(DateTime monthStart, DateTime monthEnd, int weekIndex)
    {
        var weekStart = monthStart.Date.AddDays(weekIndex * 7);
        var weekEnd = weekStart.AddDays(6);
        if (weekEnd > monthEnd.Date)
            weekEnd = monthEnd.Date;

        var dates = new List<DateTime>();
        for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
            dates.Add(date);

        return dates;
    }

    private static (DateTime First, DateTime Second)? FindLowestImpactConsecutivePair(
        List<DateTime> dates,
        Guid employeeId,
        Dictionary<Guid, HashSet<Guid>> roleIdsByEmployee,
        Dictionary<(DateTime Date, Guid WorkRoleId), int> demandByDateAndRole,
        Dictionary<Guid, HashSet<DateTime>> availabilityByEmployee)
    {
        return dates
            .OrderBy(d => d)
            .Zip(dates.OrderBy(d => d).Skip(1), (first, second) => (first, second))
            .Where(pair => pair.second == pair.first.AddDays(1))
            .OrderBy(pair =>
                GetRestImpactScore(employeeId, pair.first, roleIdsByEmployee, demandByDateAndRole, availabilityByEmployee)
                + GetRestImpactScore(employeeId, pair.second, roleIdsByEmployee, demandByDateAndRole, availabilityByEmployee))
            .ThenBy(pair => pair.first.Day)
            .Select(pair => ((DateTime First, DateTime Second)?)pair)
            .FirstOrDefault();
    }

    private static int GetRestImpactScore(
        Guid employeeId,
        DateTime date,
        Dictionary<Guid, HashSet<Guid>> roleIdsByEmployee,
        Dictionary<(DateTime Date, Guid WorkRoleId), int> demandByDateAndRole,
        Dictionary<Guid, HashSet<DateTime>> availabilityByEmployee)
    {
        var score = 0;
        if (roleIdsByEmployee.TryGetValue(employeeId, out var roleIds))
        {
            foreach (var roleId in roleIds)
            {
                if (demandByDateAndRole.TryGetValue((date.Date, roleId), out var demand))
                    score += demand;
            }
        }

        if (availabilityByEmployee.TryGetValue(employeeId, out var unavailableDates)
            && unavailableDates.Contains(date.Date))
            score -= 100;

        return score;
    }

    private static List<ShiftGenerationWarningDto> BuildRestComplianceWarnings(
        List<Employee> employees,
        Dictionary<Guid, EmployeeRestPlan> restPlans,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee,
        DateTime monthStart,
        DateTime monthEnd,
        int daysInMonth,
        bool includeQuotaWarning = false)
    {
        var warnings = new List<ShiftGenerationWarningDto>();

        foreach (var employee in employees.Where(e => e.ContractType != Domain.Enums.ContractType.PartTime))
        {
            var assignedDates = assignedDatesByEmployee.TryGetValue(employee.Id, out var dates)
                ? dates
                : new HashSet<DateTime>();

            var assignedDays = assignedDates.Count;
            var requiredWorkingDays = Math.Max(0, daysInMonth - employee.FreeDaysPerMonth);
            var actualFreeDays = daysInMonth - assignedDays;

            if (includeQuotaWarning && actualFreeDays != employee.FreeDaysPerMonth)
            {
                warnings.Add(new ShiftGenerationWarningDto
                {
                    Date = monthStart,
                    DayOfWeek = monthStart.DayOfWeek,
                    WorkAreaName = "Cuota empleado",
                    WorkRoleName = "(No aplica)",
                    RequiredCount = requiredWorkingDays,
                    AssignedCount = assignedDays,
                    Reason = $"Empleado {employee.FirstName} {employee.LastName}: configurado para {employee.FreeDaysPerMonth} dias libres al mes, obtuvo {actualFreeDays}. Trabajo {assignedDays} de {requiredWorkingDays} dias esperados."
                });
            }

            if (restPlans.TryGetValue(employee.Id, out var plan))
            {
                var preferredWorked = plan.PreferredRestDates
                    .Where(assignedDates.Contains)
                    .OrderBy(d => d)
                    .ToList();

                if (preferredWorked.Count > 0)
                {
                    warnings.Add(new ShiftGenerationWarningDto
                    {
                        Date = preferredWorked.First(),
                        DayOfWeek = preferredWorked.First().DayOfWeek,
                        WorkAreaName = "Descanso empleado",
                        WorkRoleName = "(No aplica)",
                        RequiredCount = 0,
                        AssignedCount = 0,
                        Reason = $"Empleado {employee.FirstName} {employee.LastName}: no se pudieron respetar {preferredWorked.Count} descanso(s) preferidos por cobertura. Fechas trabajadas que eran descanso sugerido: {string.Join(", ", preferredWorked.Select(d => d.ToString("dd/MM")))}."
                    });
                }

                foreach (var target in plan.WeeklyRestTargets.Where(t => t.Value >= 2))
                {
                    var weekDates = GetDatesForWeekIndex(monthStart, monthEnd, target.Key);
                    var freeDates = weekDates
                        .Where(d => !assignedDates.Contains(d))
                        .OrderBy(d => d)
                        .ToList();

                    var hasConsecutiveRest = freeDates
                        .Zip(freeDates.Skip(1), (first, second) => (first, second))
                        .Any(pair => pair.second == pair.first.AddDays(1));

                    if (!hasConsecutiveRest)
                    {
                        warnings.Add(new ShiftGenerationWarningDto
                        {
                            Date = weekDates.First(),
                            DayOfWeek = weekDates.First().DayOfWeek,
                            WorkAreaName = "Descanso empleado",
                            WorkRoleName = "(No aplica)",
                            RequiredCount = target.Value,
                            AssignedCount = freeDates.Count,
                            Reason = $"Empleado {employee.FirstName} {employee.LastName}: la semana {target.Key + 1} necesitaba {target.Value} dias libres y se intento que fueran consecutivos, pero no quedo ningun bloque consecutivo. Descansos reales: {(freeDates.Count == 0 ? "ninguno" : string.Join(", ", freeDates.Select(d => d.ToString("dd/MM"))))}."
                        });
                    }
                }
            }

            var maxRun = GetMaxConsecutiveAssignedDays(assignedDates, monthStart, monthEnd);
            if (maxRun > MaxConsecutiveWorkDays)
            {
                warnings.Add(new ShiftGenerationWarningDto
                {
                    Date = monthStart,
                    DayOfWeek = monthStart.DayOfWeek,
                    WorkAreaName = "Descanso empleado",
                    WorkRoleName = "(No aplica)",
                    RequiredCount = MaxConsecutiveWorkDays,
                    AssignedCount = maxRun,
                    Reason = $"Empleado {employee.FirstName} {employee.LastName}: quedo con una racha de {maxRun} dias trabajados seguidos, superior al maximo permitido de {MaxConsecutiveWorkDays}."
                });
            }
        }

        return warnings;
    }

    private static int GetMaxConsecutiveAssignedDays(HashSet<DateTime> assignedDates, DateTime monthStart, DateTime monthEnd)
    {
        var max = 0;
        var current = 0;
        for (var date = monthStart.Date; date <= monthEnd.Date; date = date.AddDays(1))
        {
            if (assignedDates.Contains(date))
            {
                current++;
                max = Math.Max(max, current);
            }
            else
            {
                current = 0;
            }
        }

        return max;
    }

    private static List<(DateTime Date, DayOfWeek DayOfWeek, Guid WorkAreaId, Guid WorkRoleId, TimeSpan StartTime, TimeSpan EndTime, TimeSpan? BreakDuration, TimeSpan? LunchDuration, string? Notes, string WorkAreaName, string WorkRoleName, int MissingCount)> AutoAssignUncoveredSlots(
        List<(DateTime Date, DayOfWeek DayOfWeek, Guid WorkAreaId, Guid WorkRoleId, TimeSpan StartTime, TimeSpan EndTime, TimeSpan? BreakDuration, TimeSpan? LunchDuration, string? Notes, string WorkAreaName, string WorkRoleName, int MissingCount)> uncoveredSlots,
        Guid branchId,
        Dictionary<Guid, List<EmployeeWorkRole>> roleCandidates,
        Dictionary<Guid, int> roleCountByEmployee,
        Dictionary<Guid, HashSet<DateTime>> availabilityByEmployee,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee,
        Dictionary<Guid, decimal> hoursByEmployee,
        Dictionary<Guid, Dictionary<int, decimal>> weeklyHoursByEmployee,
        int daysInMonth,
        DateTime monthStart,
        List<ShiftAssignment> assignmentsToCreate)
    {
        var unresolved = new List<(DateTime Date, DayOfWeek DayOfWeek, Guid WorkAreaId, Guid WorkRoleId, TimeSpan StartTime, TimeSpan EndTime, TimeSpan? BreakDuration, TimeSpan? LunchDuration, string? Notes, string WorkAreaName, string WorkRoleName, int MissingCount)>();

        foreach (var slot in uncoveredSlots.Where(s => s.MissingCount > 0))
        {
            if (!roleCandidates.TryGetValue(slot.WorkRoleId, out var candidatesForRole))
            {
                unresolved.Add(slot);
                continue;
            }

            var remaining = slot.MissingCount;

            for (var i = 0; i < slot.MissingCount; i++)
            {
                var eligibleCandidates = candidatesForRole
                    .Where(c => c.Employee != null)
                    .Where(c => IsEmployeeAvailable(c.Employee!.Id, slot.Date, availabilityByEmployee))
                    .Where(c => !IsEmployeeAlreadyAssigned(c.Employee!.Id, slot.Date, assignedDatesByEmployee))
                    .Where(c => CanAssignByFreeDays(c, daysInMonth, assignedDatesByEmployee))
                    .Where(c => CanAssignByMaxConsecutiveDays(c.Employee!.Id, slot.Date, assignedDatesByEmployee))
                    .Where(c => CanAssignByHours(c.Employee!, slot.Date, monthStart, slot.StartTime, slot.EndTime, slot.BreakDuration, slot.LunchDuration, weeklyHoursByEmployee))
                    .OrderByDescending(c => c.IsPrimary)
                    .ThenBy(c => c.Priority)
                    .ThenBy(c => GetEmployeeRoleCount(c.EmployeeId, roleCountByEmployee))
                    .ThenByDescending(c => GetRemainingDaysToAssign(c, daysInMonth, assignedDatesByEmployee))
                    .ThenBy(c => WillUseWeeklyExtraHours(c.Employee!, slot.Date, monthStart, slot.StartTime, slot.EndTime, slot.BreakDuration, slot.LunchDuration, weeklyHoursByEmployee))
                    .ThenBy(c => GetEmployeeHours(c.Employee!.Id, hoursByEmployee))
                    .ThenBy(c => GetEmployeeAssignedDays(c.Employee!.Id, assignedDatesByEmployee))
                    .ToList();

                var selected = eligibleCandidates.FirstOrDefault();
                if (selected == null)
                    break;

                var employeeId = selected.Employee!.Id;
                var workedHours = CalculateWorkedHours(slot.StartTime, slot.EndTime, slot.BreakDuration, slot.LunchDuration);

                assignmentsToCreate.Add(new ShiftAssignment
                {
                    BranchId = branchId,
                    EmployeeId = employeeId,
                    Date = slot.Date,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    BreakDuration = slot.BreakDuration,
                    LunchDuration = slot.LunchDuration,
                    WorkAreaId = slot.WorkAreaId,
                    WorkRoleId = slot.WorkRoleId,
                    WorkedHours = workedHours,
                    Notes = slot.Notes,
                    IsApproved = false,
                });

                TrackAssignment(employeeId, slot.Date, workedHours, monthStart, assignedDatesByEmployee, hoursByEmployee, weeklyHoursByEmployee);
                remaining--;
            }

            if (remaining > 0)
            {
                unresolved.Add((
                    Date: slot.Date,
                    DayOfWeek: slot.DayOfWeek,
                    WorkAreaId: slot.WorkAreaId,
                    WorkRoleId: slot.WorkRoleId,
                    StartTime: slot.StartTime,
                    EndTime: slot.EndTime,
                    BreakDuration: slot.BreakDuration,
                    LunchDuration: slot.LunchDuration,
                    Notes: slot.Notes,
                    WorkAreaName: slot.WorkAreaName,
                    WorkRoleName: slot.WorkRoleName,
                    MissingCount: remaining));
            }
        }

        return unresolved;
    }

    private static bool CanAssignByFreeDays(
        EmployeeWorkRole ewr,
        int daysInMonth,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        if (ewr.Employee == null)
            return false;

        if (ewr.Employee.ContractType == Domain.Enums.ContractType.PartTime)
            return true;

        var freeDaysPerMonth = ewr.Employee.FreeDaysPerMonth;
        var requiredWorkingDays = Math.Max(0, daysInMonth - freeDaysPerMonth);
        var assignedDays = GetEmployeeAssignedDays(ewr.EmployeeId, assignedDatesByEmployee);

        return assignedDays < requiredWorkingDays;
    }

    private static int GetEmployeeRoleCount(Guid employeeId, Dictionary<Guid, int> roleCountByEmployee)
        => roleCountByEmployee.TryGetValue(employeeId, out var count) ? count : int.MaxValue;

    private static int GetRemainingDaysToAssign(EmployeeWorkRole ewr, int daysInMonth, Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        if (ewr.Employee == null)
            return 0;

        if (ewr.Employee.ContractType == Domain.Enums.ContractType.PartTime)
        {
            var assignedDays = GetEmployeeAssignedDays(ewr.EmployeeId, assignedDatesByEmployee);
            return Math.Max(0, daysInMonth - assignedDays);
        }

        var requiredWorkingDays = Math.Max(0, daysInMonth - ewr.Employee.FreeDaysPerMonth);
        var assignedDaysForEmployee = GetEmployeeAssignedDays(ewr.EmployeeId, assignedDatesByEmployee);
        return Math.Max(0, requiredWorkingDays - assignedDaysForEmployee);
    }

    private static List<ShiftGenerationWarningDto> BuildPreGenerationWarnings(
        DateTime generationStartDate,
        DateTime endDate,
        int daysInMonth,
        List<ShiftTemplate> shiftTemplates,
        List<SpecialDate> specialDates,
        Dictionary<Guid, WorkRole> specialDateRoles,
        Dictionary<Guid, List<EmployeeWorkRole>> roleCandidates,
        Dictionary<Guid, HashSet<DateTime>> availabilityByEmployee,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        var warnings = new List<ShiftGenerationWarningDto>();
        var remainingDays = (endDate.Date - generationStartDate.Date).Days + 1;
        var specialDateDict = specialDates.ToDictionary(sd => sd.Date.Date);
        var requiredByRole = new Dictionary<Guid, (int Required, string WorkAreaName, string WorkRoleName)>();
        var requiredHoursByRole = new Dictionary<Guid, decimal>();

        for (var day = 0; day < remainingDays; day++)
        {
            var currentDate = generationStartDate.AddDays(day);
            var dayOfWeek = currentDate.DayOfWeek;

            List<(Guid WorkRoleId, int RequiredCount, string WorkAreaName, string WorkRoleName, decimal NetHours)> templatesForDay;

            if (specialDateDict.TryGetValue(currentDate.Date, out var specialDate) && specialDate.Templates.Any())
            {
                templatesForDay = specialDate.Templates
                    .Select(t => (
                        WorkRoleId: t.WorkRoleId,
                        RequiredCount: t.RequiredCount,
                        WorkAreaName: t.WorkArea?.Name ?? "Desconocida",
                        WorkRoleName: specialDateRoles.TryGetValue(t.WorkRoleId, out var role) ? role.Name : "Desconocido",
                        NetHours: CalculateWorkedHours(t.StartTime, t.EndTime, t.BreakDuration, t.LunchDuration)))
                    .ToList();
            }
            else
            {
                templatesForDay = shiftTemplates
                    .Where(t => t.DayOfWeek == dayOfWeek)
                    .Select(t => (
                        WorkRoleId: t.WorkRoleId,
                        RequiredCount: t.RequiredCount,
                        WorkAreaName: t.WorkArea?.Name ?? "Desconocida",
                        WorkRoleName: t.WorkRole?.Name ?? "Desconocido",
                        NetHours: CalculateWorkedHours(t.StartTime, t.EndTime, t.BreakDuration, t.LunchDuration)))
                    .ToList();
            }

            foreach (var template in templatesForDay)
            {
                if (!requiredByRole.TryGetValue(template.WorkRoleId, out var current))
                {
                    requiredByRole[template.WorkRoleId] = (template.RequiredCount, template.WorkAreaName, template.WorkRoleName);
                }
                else
                {
                    requiredByRole[template.WorkRoleId] = (
                        current.Required + template.RequiredCount,
                        current.WorkAreaName,
                        current.WorkRoleName);
                }

                if (!requiredHoursByRole.ContainsKey(template.WorkRoleId))
                    requiredHoursByRole[template.WorkRoleId] = 0m;

                requiredHoursByRole[template.WorkRoleId] += template.NetHours * template.RequiredCount;
            }
        }

        foreach (var kvp in requiredByRole)
        {
            var workRoleId = kvp.Key;
            var required = kvp.Value.Required;
            var workAreaName = kvp.Value.WorkAreaName;
            var workRoleName = kvp.Value.WorkRoleName;

            if (!roleCandidates.TryGetValue(workRoleId, out var candidatesForRole) || candidatesForRole.Count == 0)
            {
                warnings.Add(new ShiftGenerationWarningDto
                {
                    Date = generationStartDate,
                    DayOfWeek = generationStartDate.DayOfWeek,
                    WorkAreaName = workAreaName,
                    WorkRoleName = workRoleName,
                    RequiredCount = required,
                    AssignedCount = 0,
                    Reason = "PreCheck: No hay empleados asignados a este rol para cubrir la demanda mensual."
                });
                continue;
            }

            var capacity = 0;
            var capacityMinHours = 0m;
            var capacityMaxHours = 0m;
            var distinctEmployees = candidatesForRole
                .Where(c => c.Employee != null)
                .GroupBy(c => c.EmployeeId)
                .Select(g => g.First());

            foreach (var candidate in distinctEmployees)
            {
                var employee = candidate.Employee!;
                var unavailableDays = availabilityByEmployee.TryGetValue(employee.Id, out var dates)
                    ? dates.Count(d => d >= generationStartDate.Date && d <= endDate.Date)
                    : 0;

                var assignedDaysSoFar = GetEmployeeAssignedDays(employee.Id, assignedDatesByEmployee);
                var availableDaysRemaining = Math.Max(0, remainingDays - unavailableDays);
                var remainingWorkingDays = employee.ContractType == Domain.Enums.ContractType.PartTime
                    ? availableDaysRemaining
                    : Math.Max(0, (daysInMonth - employee.FreeDaysPerMonth) - assignedDaysSoFar);
                var capacityDays = Math.Min(remainingWorkingDays, availableDaysRemaining);

                capacity += capacityDays;
                var minHoursPerDay = GetEffectiveWeeklyMinHours(employee) / 5m;
                var maxHoursPerDay = GetEffectiveWeeklyMaxHours(employee) / 5m;
                capacityMinHours += capacityDays * minHoursPerDay;
                capacityMaxHours += capacityDays * maxHoursPerDay;
            }

            if (capacity < required)
            {
                warnings.Add(new ShiftGenerationWarningDto
                {
                    Date = generationStartDate,
                    DayOfWeek = generationStartDate.DayOfWeek,
                    WorkAreaName = workAreaName,
                    WorkRoleName = workRoleName,
                    RequiredCount = required,
                    AssignedCount = capacity,
                    Reason = "PreCheck: Capacidad mensual insuficiente considerando dias libres y disponibilidad."
                });
            }

            if (requiredHoursByRole.TryGetValue(workRoleId, out var requiredHours))
            {
                if (capacityMaxHours < requiredHours)
                {
                    warnings.Add(new ShiftGenerationWarningDto
                    {
                        Date = generationStartDate,
                        DayOfWeek = generationStartDate.DayOfWeek,
                        WorkAreaName = workAreaName,
                        WorkRoleName = workRoleName,
                        RequiredCount = (int)Math.Ceiling(requiredHours),
                        AssignedCount = (int)Math.Floor(capacityMaxHours),
                        Reason = $"PreCheck: Horas insuficientes para cubrir la demanda aun usando horas máximas. Requeridas {requiredHours:F1}h, capacidad máxima {capacityMaxHours:F1}h."
                    });
                }
                else if (capacityMinHours < requiredHours)
                {
                    warnings.Add(new ShiftGenerationWarningDto
                    {
                        Date = generationStartDate,
                        DayOfWeek = generationStartDate.DayOfWeek,
                        WorkAreaName = workAreaName,
                        WorkRoleName = workRoleName,
                        RequiredCount = (int)Math.Ceiling(requiredHours),
                        AssignedCount = (int)Math.Floor(capacityMinHours),
                        Reason = $"PreCheck: Para cubrir la demanda se requerirá usar horas extra (sobre horas semanales objetivo). Requeridas {requiredHours:F1}h, capacidad base {capacityMinHours:F1}h, capacidad máxima {capacityMaxHours:F1}h."
                    });
                }
            }
        }

        return warnings;
    }

    private static bool CanAssignByHours(
        Employee employee,
        DateTime dateToAssign,
        DateTime monthStart,
        TimeSpan startTime,
        TimeSpan endTime,
        TimeSpan? breakDuration,
        TimeSpan? lunchDuration,
        Dictionary<Guid, Dictionary<int, decimal>> weeklyHoursByEmployee)
    {
        var maxHours = GetEffectiveWeeklyMaxHours(employee);
        var currentHours = GetEmployeeWeeklyHours(employee.Id, dateToAssign, monthStart, weeklyHoursByEmployee);
        var workedHours = CalculateWorkedHours(startTime, endTime, breakDuration, lunchDuration);
        return currentHours + workedHours <= maxHours;
    }

    private static bool WillUseWeeklyExtraHours(
        Employee employee,
        DateTime dateToAssign,
        DateTime monthStart,
        TimeSpan startTime,
        TimeSpan endTime,
        TimeSpan? breakDuration,
        TimeSpan? lunchDuration,
        Dictionary<Guid, Dictionary<int, decimal>> weeklyHoursByEmployee)
    {
        var minHours = GetEffectiveWeeklyMinHours(employee);
        var currentHours = GetEmployeeWeeklyHours(employee.Id, dateToAssign, monthStart, weeklyHoursByEmployee);
        var workedHours = CalculateWorkedHours(startTime, endTime, breakDuration, lunchDuration);
        return currentHours + workedHours > minHours;
    }

    private static decimal GetEffectiveWeeklyMinHours(Employee employee)
        => employee.WeeklyMinHours < 0m ? 0m : employee.WeeklyMinHours;

    private static decimal GetEffectiveWeeklyMaxHours(Employee employee)
    {
        var minHours = GetEffectiveWeeklyMinHours(employee);
        var maxHours = employee.WeeklyMaxHours < 0m ? 0m : employee.WeeklyMaxHours;
        return maxHours < minHours ? minHours : maxHours;
    }

    private static void TrackAssignment(
        Guid employeeId,
        DateTime date,
        decimal workedHours,
        DateTime monthStart,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee,
        Dictionary<Guid, decimal> hoursByEmployee,
        Dictionary<Guid, Dictionary<int, decimal>> weeklyHoursByEmployee)
    {
        if (!assignedDatesByEmployee.TryGetValue(employeeId, out var dates))
        {
            dates = new HashSet<DateTime>();
            assignedDatesByEmployee[employeeId] = dates;
        }

        dates.Add(date.Date);

        if (!hoursByEmployee.ContainsKey(employeeId))
            hoursByEmployee[employeeId] = 0m;

        hoursByEmployee[employeeId] += workedHours;

        var weekIndex = GetWeekIndex(date, monthStart);
        if (!weeklyHoursByEmployee.TryGetValue(employeeId, out var weeklyHours))
        {
            weeklyHours = new Dictionary<int, decimal>();
            weeklyHoursByEmployee[employeeId] = weeklyHours;
        }

        if (!weeklyHours.ContainsKey(weekIndex))
            weeklyHours[weekIndex] = 0m;

        weeklyHours[weekIndex] += workedHours;
    }

    private static decimal CalculateWorkedHours(TimeSpan startTime, TimeSpan endTime, TimeSpan? breakDuration, TimeSpan? lunchDuration)
    {
        var breakMinutes = breakDuration?.TotalMinutes ?? 0;
        var lunchMinutes = lunchDuration?.TotalMinutes ?? 0;
        var totalMinutes = (endTime - startTime).TotalMinutes - breakMinutes - lunchMinutes;
        return (decimal)(totalMinutes / 60.0);
    }

    private static decimal GetEmployeeHours(Guid employeeId, Dictionary<Guid, decimal> hoursByEmployee)
        => hoursByEmployee.TryGetValue(employeeId, out var hours) ? hours : 0m;

    private static decimal GetEmployeeWeeklyHours(
        Guid employeeId,
        DateTime date,
        DateTime monthStart,
        Dictionary<Guid, Dictionary<int, decimal>> weeklyHoursByEmployee)
    {
        var weekIndex = GetWeekIndex(date, monthStart);
        if (!weeklyHoursByEmployee.TryGetValue(employeeId, out var weeklyHours))
            return 0m;

        return weeklyHours.TryGetValue(weekIndex, out var hours) ? hours : 0m;
    }

    private static int GetWeekIndex(DateTime date, DateTime monthStart)
        => Math.Max(0, (date.Date - monthStart.Date).Days / 7);

    private static int GetEmployeeAssignedDaysInWeek(
        Guid employeeId,
        int weekIndex,
        DateTime monthStart,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        if (!assignedDatesByEmployee.TryGetValue(employeeId, out var dates))
            return 0;

        return dates.Count(d => GetWeekIndex(d, monthStart) == weekIndex);
    }

    private static int GetDaysInWeekIndex(DateTime monthStart, int weekIndex)
    {
        var weekStart = monthStart.Date.AddDays(weekIndex * 7);
        var weekEnd = weekStart.AddDays(6);
        var monthEnd = monthStart.Date.AddMonths(1).AddDays(-1);

        if (weekStart > monthEnd)
            return 0;

        if (weekEnd > monthEnd)
            weekEnd = monthEnd;

        return (weekEnd - weekStart).Days + 1;
    }

    private static int GetEmployeeAssignedDays(Guid employeeId, Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
        => assignedDatesByEmployee.TryGetValue(employeeId, out var dates) ? dates.Count : 0;
}
