using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Payroll.Queries;
using Grimorio.Domain.Entities.Payroll;
using Grimorio.Domain.Enums;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Payroll.Queries;

public class GetPayrollConfigurationQueryHandler : IRequestHandler<GetPayrollConfigurationQuery, PayrollConfigurationDto?>
{
    private readonly GrimorioDbContext _context;
    private readonly IMapper _mapper;

    public GetPayrollConfigurationQueryHandler(GrimorioDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PayrollConfigurationDto?> Handle(GetPayrollConfigurationQuery request, CancellationToken cancellationToken)
    {
        var config = await _context.PayrollConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(pc => pc.BranchId == request.BranchId && !pc.IsDeleted, cancellationToken);

        if (config == null)
            return null;

        return _mapper.Map<PayrollConfigurationDto>(config);
    }
}

public class GetPayrollSummaryQueryHandler : IRequestHandler<GetPayrollSummaryQuery, List<EmployeePayrollSummaryDto>>
{
    private readonly GrimorioDbContext _context;

    public GetPayrollSummaryQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<EmployeePayrollSummaryDto>> Handle(GetPayrollSummaryQuery request, CancellationToken cancellationToken)
    {
        var startDate = new DateTime(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var config = await _context.PayrollConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(pc => pc.BranchId == request.BranchId && !pc.IsDeleted, cancellationToken)
            ?? new PayrollConfiguration { BranchId = request.BranchId };

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.BranchId == request.BranchId && e.IsActive && !e.IsDeleted)
            .Include(e => e.Position)
            .OrderBy(e => e.FirstName)
            .ToListAsync(cancellationToken);

        var advances = await _context.PayrollAdvances
            .AsNoTracking()
            .Where(a => a.BranchId == request.BranchId && !a.IsDeleted && a.Date >= startDate && a.Date <= endDate)
            .ToListAsync(cancellationToken);

        var consumptions = await _context.EmployeeConsumptions
            .AsNoTracking()
            .Where(c => c.BranchId == request.BranchId && !c.IsDeleted && c.Date >= startDate && c.Date <= endDate)
            .ToListAsync(cancellationToken);

        var adjustments = await _context.PayrollAdjustments
            .AsNoTracking()
            .Where(a => a.BranchId == request.BranchId && !a.IsDeleted && a.Date >= startDate && a.Date <= endDate)
            .ToListAsync(cancellationToken);

        var advancesByEmployee = advances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var consumptionsByEmployee = consumptions
            .GroupBy(c => c.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var adjustmentsByEmployee = adjustments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var summaries = new List<EmployeePayrollSummaryDto>();
        foreach (var employee in employees)
        {
            var baseSalary = employee.BaseSalary;
            var iessEmployee = CalculatePercent(baseSalary, config.IessEmployeeRate);
            var iessEmployer = CalculatePercent(baseSalary, config.IessEmployerRate);
            var incomeTax = CalculatePercent(baseSalary, config.IncomeTaxRate);
            var decimoThird = employee.DecimoThirdMonthly ? CalculatePercent(baseSalary, config.DecimoThirdRate) : 0m;
            var decimoFourth = employee.DecimoFourthMonthly ? CalculatePercent(baseSalary, config.DecimoFourthRate) : 0m;
            var reserveFund = employee.ReserveFundMonthly ? CalculatePercent(baseSalary, config.ReserveFundRate) : 0m;

            var overtime50 = 0m;
            var overtime100 = 0m;
            var otherIncome = 0m;
            var otherDeductions = 0m;

            if (adjustmentsByEmployee.TryGetValue(employee.Id, out var employeeAdjustments))
            {
                foreach (var adjustment in employeeAdjustments)
                {
                    var amount = ResolveAdjustmentAmount(adjustment, baseSalary, config);

                    if (adjustment.Type == PayrollAdjustmentType.Income)
                    {
                        if (adjustment.Category == PayrollAdjustmentCategory.Overtime50)
                            overtime50 += amount;
                        else if (adjustment.Category == PayrollAdjustmentCategory.Overtime100)
                            overtime100 += amount;
                        else
                            otherIncome += amount;
                    }
                    else
                    {
                        otherDeductions += amount;
                    }
                }
            }

            var advancesTotal = advancesByEmployee.TryGetValue(employee.Id, out var adv) ? adv : 0m;
            var consumptionsTotal = consumptionsByEmployee.TryGetValue(employee.Id, out var cons) ? cons : 0m;

            var totalIncome = baseSalary + decimoThird + decimoFourth + reserveFund + overtime50 + overtime100 + otherIncome;
            var totalDeductions = iessEmployee + incomeTax + otherDeductions + advancesTotal + consumptionsTotal;
            var netPay = totalIncome - totalDeductions;

            summaries.Add(new EmployeePayrollSummaryDto
            {
                EmployeeId = employee.Id,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                PositionName = employee.Position?.Name ?? string.Empty,
                BankAccount = employee.BankAccount,
                BaseSalary = baseSalary,
                IessEmployee = iessEmployee,
                IessEmployer = iessEmployer,
                IncomeTax = incomeTax,
                DecimoThird = decimoThird,
                DecimoFourth = decimoFourth,
                ReserveFund = reserveFund,
                Overtime50 = overtime50,
                Overtime100 = overtime100,
                OtherIncome = otherIncome,
                OtherDeductions = otherDeductions,
                Advances = advancesTotal,
                Consumptions = consumptionsTotal,
                TotalIncome = totalIncome,
                TotalDeductions = totalDeductions,
                NetPay = netPay
            });
        }

        return summaries;
    }

    private static decimal CalculatePercent(decimal baseValue, decimal rate)
    {
        return Math.Round(baseValue * rate / 100m, 2);
    }

    private static decimal ResolveAdjustmentAmount(PayrollAdjustment adjustment, decimal baseSalary, PayrollConfiguration config)
    {
        var isOvertime = adjustment.Category == PayrollAdjustmentCategory.Overtime50
            || adjustment.Category == PayrollAdjustmentCategory.Overtime100;

        if (isOvertime)
        {
            if (!adjustment.Hours.HasValue)
                return adjustment.Amount ?? 0m;

            var monthlyHours = config.MonthlyHours > 0 ? config.MonthlyHours : 240;
            var hourlyRate = baseSalary / monthlyHours;
            var surchargeRate = adjustment.Category == PayrollAdjustmentCategory.Overtime100
                ? config.OvertimeRate100
                : config.OvertimeRate50;

            return Math.Round(adjustment.Hours.Value * hourlyRate * (1m + surchargeRate / 100m), 2);
        }

        return adjustment.Amount ?? 0m;
    }
}

public class GetPayrollAdvancesQueryHandler : IRequestHandler<GetPayrollAdvancesQuery, List<PayrollAdvanceDto>>
{
    private readonly GrimorioDbContext _context;
    private readonly IMapper _mapper;

    public GetPayrollAdvancesQueryHandler(GrimorioDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<PayrollAdvanceDto>> Handle(GetPayrollAdvancesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PayrollAdvances
            .AsNoTracking()
            .Where(a => a.BranchId == request.BranchId && !a.IsDeleted);

        if (request.EmployeeId.HasValue)
            query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);

        if (request.Year.HasValue && request.Month.HasValue)
        {
            var startDate = new DateTime(request.Year.Value, request.Month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            query = query.Where(a => a.Date >= startDate && a.Date <= endDate);
        }

        var advances = await query
            .OrderByDescending(a => a.Date)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<PayrollAdvanceDto>>(advances);
    }
}

public class GetEmployeeConsumptionsQueryHandler : IRequestHandler<GetEmployeeConsumptionsQuery, List<EmployeeConsumptionDto>>
{
    private readonly GrimorioDbContext _context;
    private readonly IMapper _mapper;

    public GetEmployeeConsumptionsQueryHandler(GrimorioDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<EmployeeConsumptionDto>> Handle(GetEmployeeConsumptionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.EmployeeConsumptions
            .AsNoTracking()
            .Where(c => c.BranchId == request.BranchId && !c.IsDeleted);

        if (request.EmployeeId.HasValue)
            query = query.Where(c => c.EmployeeId == request.EmployeeId.Value);

        if (request.Year.HasValue && request.Month.HasValue)
        {
            var startDate = new DateTime(request.Year.Value, request.Month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            query = query.Where(c => c.Date >= startDate && c.Date <= endDate);
        }

        var consumptions = await query
            .OrderByDescending(c => c.Date)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<EmployeeConsumptionDto>>(consumptions);
    }
}

public class GetPayrollAdjustmentsQueryHandler : IRequestHandler<GetPayrollAdjustmentsQuery, List<PayrollAdjustmentDto>>
{
    private readonly GrimorioDbContext _context;
    private readonly IMapper _mapper;

    public GetPayrollAdjustmentsQueryHandler(GrimorioDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<PayrollAdjustmentDto>> Handle(GetPayrollAdjustmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PayrollAdjustments
            .AsNoTracking()
            .Where(a => a.BranchId == request.BranchId && !a.IsDeleted);

        if (request.EmployeeId.HasValue)
            query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);

        if (request.Year.HasValue && request.Month.HasValue)
        {
            var startDate = new DateTime(request.Year.Value, request.Month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            query = query.Where(a => a.Date >= startDate && a.Date <= endDate);
        }

        var adjustments = await query
            .OrderByDescending(a => a.Date)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<PayrollAdjustmentDto>>(adjustments);
    }
}

public class GetPayrollRolesByEmployeeQueryHandler : IRequestHandler<GetPayrollRolesByEmployeeQuery, List<PayrollRoleDto>>
{
    private readonly GrimorioDbContext _context;

    public GetPayrollRolesByEmployeeQueryHandler(GrimorioDbContext context)
    {
        _context = context;
    }

    public async Task<List<PayrollRoleDto>> Handle(GetPayrollRolesByEmployeeQuery request, CancellationToken cancellationToken)
    {
        var roles = await _context.PayrollRoleHeaders
            .AsNoTracking()
            .Where(r => r.BranchId == request.BranchId && r.EmployeeId == request.EmployeeId && !r.IsDeleted)
            .Include(r => r.Employee)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .Select(r => new PayrollRoleDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : string.Empty,
                Year = r.Year,
                Month = r.Month,
                Status = r.Status,
                GeneratedAt = r.GeneratedAt,
                AuthorizedAt = r.AuthorizedAt,
                PaidAt = r.PaidAt,
                TotalIncome = r.TotalIncome,
                TotalDeductions = r.TotalDeductions,
                NetPay = r.NetPay
            })
            .ToListAsync(cancellationToken);

        return roles;
    }
}

public class GetPayrollRoleDetailQueryHandler : IRequestHandler<GetPayrollRoleDetailQuery, PayrollRoleFullDto?>
{
    private readonly GrimorioDbContext _context;

    public GetPayrollRoleDetailQueryHandler(GrimorioDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollRoleFullDto?> Handle(GetPayrollRoleDetailQuery request, CancellationToken cancellationToken)
    {
        var role = await _context.PayrollRoleHeaders
            .AsNoTracking()
            .Where(r => r.Id == request.PayrollRoleId && r.BranchId == request.BranchId && !r.IsDeleted)
            .Include(r => r.Employee)
            .Include(r => r.Details.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        if (role == null)
            return null;

        return new PayrollRoleFullDto
        {
            Header = new PayrollRoleDto
            {
                Id = role.Id,
                EmployeeId = role.EmployeeId,
                EmployeeName = role.Employee != null ? $"{role.Employee.FirstName} {role.Employee.LastName}" : string.Empty,
                Year = role.Year,
                Month = role.Month,
                Status = role.Status,
                GeneratedAt = role.GeneratedAt,
                AuthorizedAt = role.AuthorizedAt,
                PaidAt = role.PaidAt,
                TotalIncome = role.TotalIncome,
                TotalDeductions = role.TotalDeductions,
                NetPay = role.NetPay
            },
            Details = role.Details
                .OrderBy(d => d.SortOrder)
                .Select(d => new PayrollRoleDetailDto
                {
                    Id = d.Id,
                    PayrollRoleHeaderId = d.PayrollRoleHeaderId,
                    Type = d.Type,
                    Concept = d.Concept,
                    Amount = d.Amount,
                    SortOrder = d.SortOrder
                })
                .ToList()
        };
    }
}
