using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Queries;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Queries;

public class GetWorkAreasQueryHandler : IRequestHandler<GetWorkAreasQuery, List<WorkAreaDto>>
{
    private readonly GrimorioDbContext _context;

    public GetWorkAreasQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<WorkAreaDto>> Handle(GetWorkAreasQuery request, CancellationToken cancellationToken)
    {
        return await _context.WorkAreas
            .Where(w => w.BranchId == request.BranchId && !w.IsDeleted)
            .OrderBy(w => w.DisplayOrder)
            .Include(w => w.WorkRoles.Where(wr => !wr.IsDeleted))
            .Select(w => new WorkAreaDto
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                Color = w.Color,
                DisplayOrder = w.DisplayOrder,
                BranchId = w.BranchId,
                WorkRoles = w.WorkRoles
                    .Select(wr => new WorkRoleDto
                    {
                        Id = wr.Id,
                        Name = wr.Name,
                        Description = wr.Description,
                        WorkAreaId = wr.WorkAreaId,
                        FreeDaysPerMonth = wr.FreeDaysPerMonth,
                        DailyHoursTarget = wr.DailyHoursTarget
                    }).ToList()
            }).ToListAsync(cancellationToken);
    }
}

public class GetWorkAreaByIdQueryHandler : IRequestHandler<GetWorkAreaByIdQuery, WorkAreaDto?>
{
    private readonly GrimorioDbContext _context;

    public GetWorkAreaByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkAreaDto?> Handle(GetWorkAreaByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.WorkAreas
            .Where(w => w.Id == request.Id && !w.IsDeleted)
            .Include(w => w.WorkRoles.Where(wr => !wr.IsDeleted))
            .Select(w => new WorkAreaDto
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                Color = w.Color,
                DisplayOrder = w.DisplayOrder,
                BranchId = w.BranchId,
                WorkRoles = w.WorkRoles
                    .Select(wr => new WorkRoleDto
                    {
                        Id = wr.Id,
                        Name = wr.Name,
                        Description = wr.Description,
                        WorkAreaId = wr.WorkAreaId,
                        FreeDaysPerMonth = wr.FreeDaysPerMonth,
                        DailyHoursTarget = wr.DailyHoursTarget
                    }).ToList()
            }).FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetWorkRolesQueryHandler : IRequestHandler<GetWorkRolesQuery, List<WorkRoleDto>>
{
    private readonly GrimorioDbContext _context;

    public GetWorkRolesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<WorkRoleDto>> Handle(GetWorkRolesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.WorkRoles
            .Where(wr => !wr.IsDeleted);

        if (request.WorkAreaId.HasValue)
            query = query.Where(wr => wr.WorkAreaId == request.WorkAreaId.Value);

        return await query
            .OrderBy(wr => wr.Name)
            .Select(wr => new WorkRoleDto
            {
                Id = wr.Id,
                Name = wr.Name,
                Description = wr.Description,
                WorkAreaId = wr.WorkAreaId,
                FreeDaysPerMonth = wr.FreeDaysPerMonth,
                DailyHoursTarget = wr.DailyHoursTarget
            }).ToListAsync(cancellationToken);
    }
}

public class GetWorkRoleByIdQueryHandler : IRequestHandler<GetWorkRoleByIdQuery, WorkRoleDto?>
{
    private readonly GrimorioDbContext _context;

    public GetWorkRoleByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkRoleDto?> Handle(GetWorkRoleByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.WorkRoles
            .Where(wr => wr.Id == request.Id && !wr.IsDeleted)
            .Select(wr => new WorkRoleDto
            {
                Id = wr.Id,
                Name = wr.Name,
                Description = wr.Description,
                WorkAreaId = wr.WorkAreaId,
                FreeDaysPerMonth = wr.FreeDaysPerMonth,
                DailyHoursTarget = wr.DailyHoursTarget
            }).FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetEmployeeWorkRolesQueryHandler : IRequestHandler<GetEmployeeWorkRolesQuery, List<EmployeeWorkRoleDto>>
{
    private readonly GrimorioDbContext _context;

    public GetEmployeeWorkRolesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<EmployeeWorkRoleDto>> Handle(GetEmployeeWorkRolesQuery request, CancellationToken cancellationToken)
    {
        return await _context.EmployeeWorkRoles
            .Where(ewr => ewr.EmployeeId == request.EmployeeId && !ewr.IsDeleted)
            .Include(ewr => ewr.WorkRole)
            .ThenInclude(wr => wr!.WorkArea)
            .Select(ewr => new EmployeeWorkRoleDto
            {
                Id = ewr.Id,
                EmployeeId = ewr.EmployeeId,
                WorkRoleId = ewr.WorkRoleId,
                WorkRoleName = ewr.WorkRole!.Name,
                WorkAreaName = ewr.WorkRole!.WorkArea!.Name,
                IsPrimary = ewr.IsPrimary,
                Priority = ewr.Priority
            }).OrderByDescending(ewr => ewr.IsPrimary)
            .ThenBy(ewr => ewr.Priority)
            .ToListAsync(cancellationToken);
    }
}

public class GetShiftTemplatesQueryHandler : IRequestHandler<GetShiftTemplatesQuery, List<ShiftTemplateDto>>
{
    private readonly GrimorioDbContext _context;

    public GetShiftTemplatesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<ShiftTemplateDto>> Handle(GetShiftTemplatesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ShiftTemplates
            .Where(st => st.BranchId == request.BranchId && !st.IsDeleted);

        if (request.DayOfWeek.HasValue)
            query = query.Where(st => st.DayOfWeek == request.DayOfWeek.Value);

        return await query
            .Include(st => st.WorkArea)
            .Include(st => st.WorkRole)
            .OrderBy(st => st.DayOfWeek)
            .ThenBy(st => st.StartTime)
            .Select(st => new ShiftTemplateDto
            {
                Id = st.Id,
                BranchId = st.BranchId,
                DayOfWeek = st.DayOfWeek,
                StartTime = st.StartTime,
                EndTime = st.EndTime,
                BreakDuration = st.BreakDuration,
                LunchDuration = st.LunchDuration,
                WorkAreaId = st.WorkAreaId,
                WorkAreaName = st.WorkArea!.Name,
                WorkRoleId = st.WorkRoleId,
                WorkRoleName = st.WorkRole!.Name,
                RequiredCount = st.RequiredCount,
                Notes = st.Notes
            }).ToListAsync(cancellationToken);
    }
}

public class GetShiftTemplateByIdQueryHandler : IRequestHandler<GetShiftTemplateByIdQuery, ShiftTemplateDto?>
{
    private readonly GrimorioDbContext _context;

    public GetShiftTemplateByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftTemplateDto?> Handle(GetShiftTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.ShiftTemplates
            .Where(st => st.Id == request.Id && !st.IsDeleted)
            .Include(st => st.WorkArea)
            .Include(st => st.WorkRole)
            .Select(st => new ShiftTemplateDto
            {
                Id = st.Id,
                BranchId = st.BranchId,
                DayOfWeek = st.DayOfWeek,
                StartTime = st.StartTime,
                EndTime = st.EndTime,
                BreakDuration = st.BreakDuration,
                LunchDuration = st.LunchDuration,
                WorkAreaId = st.WorkAreaId,
                WorkAreaName = st.WorkArea!.Name,
                WorkRoleId = st.WorkRoleId,
                WorkRoleName = st.WorkRole!.Name,
                RequiredCount = st.RequiredCount,
                Notes = st.Notes
            }).FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetMonthlyShiftsQueryHandler : IRequestHandler<GetMonthlyShiftsQuery, List<ShiftAssignmentDto>>
{
    private readonly GrimorioDbContext _context;

    public GetMonthlyShiftsQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<ShiftAssignmentDto>> Handle(GetMonthlyShiftsQuery request, CancellationToken cancellationToken)
    {
        var startDate = new DateTime(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.ShiftAssignments
            .Where(sa => sa.Date >= startDate && sa.Date <= endDate && !sa.IsDeleted)
            .Include(sa => sa.Employee)
            .Include(sa => sa.WorkArea)
            .Include(sa => sa.WorkRole)
            .OrderBy(sa => sa.Date)
            .ThenBy(sa => sa.StartTime)
            .Select(sa => new ShiftAssignmentDto
            {
                Id = sa.Id,
                EmployeeId = sa.EmployeeId,
                EmployeeName = sa.Employee!.FirstName + " " + sa.Employee!.LastName,
                Date = sa.Date,
                StartTime = sa.StartTime,
                EndTime = sa.EndTime,
                BreakDuration = sa.BreakDuration,
                LunchDuration = sa.LunchDuration,
                WorkAreaId = sa.WorkAreaId,
                WorkAreaName = sa.WorkArea!.Name,
                WorkAreaColor = sa.WorkArea!.Color,
                WorkRoleId = sa.WorkRoleId,
                WorkRoleName = sa.WorkRole!.Name,
                WorkedHours = sa.WorkedHours,
                Notes = sa.Notes,
                IsApproved = sa.IsApproved,
                ApprovedBy = sa.ApprovedBy,
                ApprovedAt = sa.ApprovedAt
            }).ToListAsync(cancellationToken);
    }
}

public class GetEmployeeMonthlyShiftsQueryHandler : IRequestHandler<GetEmployeeMonthlyShiftsQuery, List<ShiftAssignmentDto>>
{
    private readonly GrimorioDbContext _context;

    public GetEmployeeMonthlyShiftsQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<ShiftAssignmentDto>> Handle(GetEmployeeMonthlyShiftsQuery request, CancellationToken cancellationToken)
    {
        var startDate = new DateTime(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.ShiftAssignments
            .Where(sa => sa.EmployeeId == request.EmployeeId && sa.Date >= startDate && sa.Date <= endDate && !sa.IsDeleted)
            .Include(sa => sa.Employee)
            .Include(sa => sa.WorkArea)
            .Include(sa => sa.WorkRole)
            .OrderBy(sa => sa.Date)
            .Select(sa => new ShiftAssignmentDto
            {
                Id = sa.Id,
                EmployeeId = sa.EmployeeId,
                EmployeeName = sa.Employee!.FirstName + " " + sa.Employee!.LastName,
                Date = sa.Date,
                StartTime = sa.StartTime,
                EndTime = sa.EndTime,
                BreakDuration = sa.BreakDuration,
                LunchDuration = sa.LunchDuration,
                WorkAreaId = sa.WorkAreaId,
                WorkAreaName = sa.WorkArea!.Name,
                WorkAreaColor = sa.WorkArea!.Color,
                WorkRoleId = sa.WorkRoleId,
                WorkRoleName = sa.WorkRole!.Name,
                WorkedHours = sa.WorkedHours,
                Notes = sa.Notes,
                IsApproved = sa.IsApproved,
                ApprovedBy = sa.ApprovedBy,
                ApprovedAt = sa.ApprovedAt
            }).ToListAsync(cancellationToken);
    }
}

public class GetShiftAssignmentByIdQueryHandler : IRequestHandler<GetShiftAssignmentByIdQuery, ShiftAssignmentDto?>
{
    private readonly GrimorioDbContext _context;

    public GetShiftAssignmentByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftAssignmentDto?> Handle(GetShiftAssignmentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.ShiftAssignments
            .Where(sa => sa.Id == request.Id && !sa.IsDeleted)
            .Include(sa => sa.Employee)
            .Include(sa => sa.WorkArea)
            .Include(sa => sa.WorkRole)
            .Select(sa => new ShiftAssignmentDto
            {
                Id = sa.Id,
                EmployeeId = sa.EmployeeId,
                EmployeeName = sa.Employee!.FirstName + " " + sa.Employee!.LastName,
                Date = sa.Date,
                StartTime = sa.StartTime,
                EndTime = sa.EndTime,
                BreakDuration = sa.BreakDuration,
                LunchDuration = sa.LunchDuration,
                WorkAreaId = sa.WorkAreaId,
                WorkAreaName = sa.WorkArea!.Name,
                WorkAreaColor = sa.WorkArea!.Color,
                WorkRoleId = sa.WorkRoleId,
                WorkRoleName = sa.WorkRole!.Name,
                WorkedHours = sa.WorkedHours,
                Notes = sa.Notes,
                IsApproved = sa.IsApproved,
                ApprovedBy = sa.ApprovedBy,
                ApprovedAt = sa.ApprovedAt
            }).FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetShiftAssignmentsByDateQueryHandler : IRequestHandler<GetShiftAssignmentsByDateQuery, List<ShiftAssignmentDto>>
{
    private readonly GrimorioDbContext _context;

    public GetShiftAssignmentsByDateQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<ShiftAssignmentDto>> Handle(GetShiftAssignmentsByDateQuery request, CancellationToken cancellationToken)
    {
        return await _context.ShiftAssignments
            .Where(sa => sa.Date == request.Date && !sa.IsDeleted)
            .Include(sa => sa.Employee)
            .Include(sa => sa.WorkArea)
            .Include(sa => sa.WorkRole)
            .OrderBy(sa => sa.StartTime)
            .Select(sa => new ShiftAssignmentDto
            {
                Id = sa.Id,
                EmployeeId = sa.EmployeeId,
                EmployeeName = sa.Employee!.FirstName + " " + sa.Employee!.LastName,
                Date = sa.Date,
                StartTime = sa.StartTime,
                EndTime = sa.EndTime,
                BreakDuration = sa.BreakDuration,
                LunchDuration = sa.LunchDuration,
                WorkAreaId = sa.WorkAreaId,
                WorkAreaName = sa.WorkArea!.Name,
                WorkAreaColor = sa.WorkArea!.Color,
                WorkRoleId = sa.WorkRoleId,
                WorkRoleName = sa.WorkRole!.Name,
                WorkedHours = sa.WorkedHours,
                Notes = sa.Notes,
                IsApproved = sa.IsApproved,
                ApprovedBy = sa.ApprovedBy,
                ApprovedAt = sa.ApprovedAt
            }).ToListAsync(cancellationToken);
    }
}

public class GetEmployeeAvailabilityQueryHandler : IRequestHandler<GetEmployeeAvailabilityQuery, List<EmployeeAvailabilityDto>>
{
    private readonly GrimorioDbContext _context;

    public GetEmployeeAvailabilityQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<EmployeeAvailabilityDto>> Handle(GetEmployeeAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var query = _context.EmployeeAvailability
            .Where(ea => ea.EmployeeId == request.EmployeeId && !ea.IsDeleted);

        if (request.Month.HasValue && request.Year.HasValue)
        {
            var startDate = new DateTime(request.Year.Value, request.Month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            query = query.Where(ea => ea.UnavailableDate >= startDate && ea.UnavailableDate <= endDate);
        }

        return await query
            .OrderBy(ea => ea.UnavailableDate)
            .Select(ea => new EmployeeAvailabilityDto
            {
                Id = ea.Id,
                EmployeeId = ea.EmployeeId,
                UnavailableDate = ea.UnavailableDate,
                Reason = ea.Reason
            }).ToListAsync(cancellationToken);
    }
}

public class GetScheduleConfigurationQueryHandler : IRequestHandler<GetScheduleConfigurationQuery, ScheduleConfigurationDto?>
{
    private readonly GrimorioDbContext _context;

    public GetScheduleConfigurationQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<ScheduleConfigurationDto?> Handle(GetScheduleConfigurationQuery request, CancellationToken cancellationToken)
    {
        var config = await _context.ScheduleConfigurations
            .FirstOrDefaultAsync(sc => sc.BranchId == request.BranchId && !sc.IsDeleted, cancellationToken);

        if (config == null)
            return null;

        return new ScheduleConfigurationDto
        {
            Id = config.Id,
            BranchId = config.BranchId,
            MinHoursPerMonth = config.MinHoursPerMonth,
            MaxHoursPerMonth = config.MaxHoursPerMonth,
            HoursMondayThursday = config.HoursMondayThursday,
            HoursFridaySaturday = config.HoursFridaySaturday,
            HoursSunday = config.HoursSunday,
            MinStaffCocina = config.MinStaffCocina,
            MinStaffCaja = config.MinStaffCaja,
            MinStaffMesas = config.MinStaffMesas,
            MinStaffBar = config.MinStaffBar,
            FreeDayColor = string.IsNullOrWhiteSpace(config.FreeDayColor) ? "#E8E8E8" : config.FreeDayColor
        };
    }
}

public class GetSchedulableEmployeesQueryHandler : IRequestHandler<GetSchedulableEmployeesQuery, List<EmployeeDto>>
{
    private readonly GrimorioDbContext _context;

    public GetSchedulableEmployeesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<EmployeeDto>> Handle(GetSchedulableEmployeesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.BranchId == request.BranchId && e.IsActive)
            .Where(e => _context.EmployeeWorkRoles.Any(ewr => ewr.EmployeeId == e.Id && !ewr.IsDeleted))
            .Include(e => e.Position)
            .OrderBy(e => e.FirstName)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                BranchId = e.BranchId,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                IdentificationNumber = e.IdentificationNumber,
                PositionId = e.PositionId,
                PositionName = e.Position != null ? e.Position.Name : string.Empty,
                HireDate = e.HireDate,
                TerminationDate = e.TerminationDate,
                IsActive = e.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetFreeEmployeesByDateQueryHandler : IRequestHandler<GetFreeEmployeesByDateQuery, List<EmployeeDto>>
{
    private readonly GrimorioDbContext _context;

    public GetFreeEmployeesByDateQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<EmployeeDto>> Handle(GetFreeEmployeesByDateQuery request, CancellationToken cancellationToken)
    {
        // Obtener todos los empleados activos de la sucursal con roles asignados
        var allEmployees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.BranchId == request.BranchId && e.IsActive)
            .Where(e => _context.EmployeeWorkRoles.Any(ewr => ewr.EmployeeId == e.Id && !ewr.IsDeleted))
            .Include(e => e.Position)
            .ToListAsync(cancellationToken);

        // Obtener empleados que tienen turno ese día
        var employeesWithShift = await _context.ShiftAssignments
            .Where(sa => sa.Date.Date == request.Date.Date && !sa.IsDeleted)
            .Select(sa => sa.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Filtrar empleados que NO tienen turno
        var freeEmployees = allEmployees
            .Where(e => !employeesWithShift.Contains(e.Id))
            .OrderBy(e => e.FirstName)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                BranchId = e.BranchId,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                IdentificationNumber = e.IdentificationNumber,
                PositionId = e.PositionId,
                PositionName = e.Position != null ? e.Position.Name : string.Empty,
                HireDate = e.HireDate,
                TerminationDate = e.TerminationDate,
                IsActive = e.IsActive
            })
            .ToList();

        return freeEmployees;
    }
}
