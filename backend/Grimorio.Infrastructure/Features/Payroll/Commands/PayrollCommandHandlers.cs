using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Text;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Payroll.Commands;
using Grimorio.Domain.Entities.Payroll;
using Grimorio.Domain.Enums;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Payroll.Commands;

public class UpsertPayrollConfigurationCommandHandler : IRequestHandler<UpsertPayrollConfigurationCommand, PayrollConfigurationDto>
{
    private readonly GrimorioDbContext _context;
    private readonly IMapper _mapper;

    public UpsertPayrollConfigurationCommandHandler(GrimorioDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PayrollConfigurationDto> Handle(UpsertPayrollConfigurationCommand request, CancellationToken cancellationToken)
    {
        var config = await _context.PayrollConfigurations
            .FirstOrDefaultAsync(pc => pc.BranchId == request.BranchId && !pc.IsDeleted, cancellationToken);

        if (config == null)
        {
            config = new PayrollConfiguration
            {
                Id = Guid.NewGuid(),
                BranchId = request.BranchId,
                IessEmployeeRate = request.IessEmployeeRate,
                IessEmployerRate = request.IessEmployerRate,
                IncomeTaxRate = request.IncomeTaxRate,
                OvertimeRate50 = request.OvertimeRate50,
                OvertimeRate100 = request.OvertimeRate100,
                DecimoThirdRate = request.DecimoThirdRate,
                DecimoFourthRate = request.DecimoFourthRate,
                ReserveFundRate = request.ReserveFundRate,
                MonthlyHours = request.MonthlyHours
            };

            _context.PayrollConfigurations.Add(config);
        }
        else
        {
            config.IessEmployeeRate = request.IessEmployeeRate;
            config.IessEmployerRate = request.IessEmployerRate;
            config.IncomeTaxRate = request.IncomeTaxRate;
            config.OvertimeRate50 = request.OvertimeRate50;
            config.OvertimeRate100 = request.OvertimeRate100;
            config.DecimoThirdRate = request.DecimoThirdRate;
            config.DecimoFourthRate = request.DecimoFourthRate;
            config.ReserveFundRate = request.ReserveFundRate;
            config.MonthlyHours = request.MonthlyHours;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PayrollConfigurationDto>(config);
    }
}

public class CreatePayrollAdvanceCommandHandler : IRequestHandler<CreatePayrollAdvanceCommand, PayrollAdvanceDto>
{
    private readonly GrimorioDbContext _context;
    private readonly IMapper _mapper;

    public CreatePayrollAdvanceCommandHandler(GrimorioDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PayrollAdvanceDto> Handle(CreatePayrollAdvanceCommand request, CancellationToken cancellationToken)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId && e.BranchId == request.BranchId && !e.IsDeleted, cancellationToken);

        if (!employeeExists)
            throw new InvalidOperationException("Empleado no encontrado.");

        var advanceMonth = request.Date.Month;
        var advanceYear = request.Date.Year;
        var advanceRole = await _context.PayrollRoleHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BranchId == request.BranchId
                && r.EmployeeId == request.EmployeeId
                && r.Year == advanceYear
                && r.Month == advanceMonth
                && !r.IsDeleted, cancellationToken);

        if (advanceRole != null && advanceRole.Status != PayrollRoleStatus.Generated)
            throw new InvalidOperationException("El rol está autorizado/pagado. No se permiten cambios.");

        var advance = new PayrollAdvance
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            Amount = request.Amount,
            Method = request.Method,
            Notes = request.Notes
        };

        _context.PayrollAdvances.Add(advance);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PayrollAdvanceDto>(advance);
    }
}

public class DeletePayrollAdvanceCommandHandler : IRequestHandler<DeletePayrollAdvanceCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeletePayrollAdvanceCommandHandler(GrimorioDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeletePayrollAdvanceCommand request, CancellationToken cancellationToken)
    {
        var advance = await _context.PayrollAdvances
            .FirstOrDefaultAsync(a => a.Id == request.PayrollAdvanceId && a.BranchId == request.BranchId && !a.IsDeleted, cancellationToken);

        if (advance == null)
            throw new InvalidOperationException("Adelanto no encontrado.");

        var role = await _context.PayrollRoleHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BranchId == request.BranchId
                && r.EmployeeId == advance.EmployeeId
                && r.Year == advance.Date.Year
                && r.Month == advance.Date.Month
                && !r.IsDeleted, cancellationToken);

        if (role != null && role.Status != PayrollRoleStatus.Generated)
            throw new InvalidOperationException("El rol está autorizado/pagado. No se permiten cambios.");

        advance.IsDeleted = true;
        advance.DeletedAt = DateTime.UtcNow;

        _context.PayrollAdvances.Update(advance);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class CreateEmployeeConsumptionCommandHandler : IRequestHandler<CreateEmployeeConsumptionCommand, EmployeeConsumptionDto>
{
    private readonly GrimorioDbContext _context;
    private readonly IMapper _mapper;

    public CreateEmployeeConsumptionCommandHandler(GrimorioDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<EmployeeConsumptionDto> Handle(CreateEmployeeConsumptionCommand request, CancellationToken cancellationToken)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId && e.BranchId == request.BranchId && !e.IsDeleted, cancellationToken);

        if (!employeeExists)
            throw new InvalidOperationException("Empleado no encontrado.");

        var consumptionMonth = request.Date.Month;
        var consumptionYear = request.Date.Year;
        var consumptionRole = await _context.PayrollRoleHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BranchId == request.BranchId
                && r.EmployeeId == request.EmployeeId
                && r.Year == consumptionYear
                && r.Month == consumptionMonth
                && !r.IsDeleted, cancellationToken);

        if (consumptionRole != null && consumptionRole.Status != PayrollRoleStatus.Generated)
            throw new InvalidOperationException("El rol está autorizado/pagado. No se permiten cambios.");

        var consumption = new EmployeeConsumption
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            Amount = request.Amount,
            Notes = request.Notes
        };

        _context.EmployeeConsumptions.Add(consumption);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<EmployeeConsumptionDto>(consumption);
    }
}

public class DeleteEmployeeConsumptionCommandHandler : IRequestHandler<DeleteEmployeeConsumptionCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteEmployeeConsumptionCommandHandler(GrimorioDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteEmployeeConsumptionCommand request, CancellationToken cancellationToken)
    {
        var consumption = await _context.EmployeeConsumptions
            .FirstOrDefaultAsync(c => c.Id == request.EmployeeConsumptionId && c.BranchId == request.BranchId && !c.IsDeleted, cancellationToken);

        if (consumption == null)
            throw new InvalidOperationException("Consumo no encontrado.");

        var role = await _context.PayrollRoleHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BranchId == request.BranchId
                && r.EmployeeId == consumption.EmployeeId
                && r.Year == consumption.Date.Year
                && r.Month == consumption.Date.Month
                && !r.IsDeleted, cancellationToken);

        if (role != null && role.Status != PayrollRoleStatus.Generated)
            throw new InvalidOperationException("El rol está autorizado/pagado. No se permiten cambios.");

        consumption.IsDeleted = true;
        consumption.DeletedAt = DateTime.UtcNow;

        _context.EmployeeConsumptions.Update(consumption);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class CreatePayrollAdjustmentCommandHandler : IRequestHandler<CreatePayrollAdjustmentCommand, PayrollAdjustmentDto>
{
    private readonly GrimorioDbContext _context;
    private readonly IMapper _mapper;

    public CreatePayrollAdjustmentCommandHandler(GrimorioDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PayrollAdjustmentDto> Handle(CreatePayrollAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId && e.BranchId == request.BranchId && !e.IsDeleted, cancellationToken);

        if (!employeeExists)
            throw new InvalidOperationException("Empleado no encontrado.");

        var adjustmentMonth = request.Date.Month;
        var adjustmentYear = request.Date.Year;
        var adjustmentRole = await _context.PayrollRoleHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BranchId == request.BranchId
                && r.EmployeeId == request.EmployeeId
                && r.Year == adjustmentYear
                && r.Month == adjustmentMonth
                && !r.IsDeleted, cancellationToken);

        if (adjustmentRole != null && adjustmentRole.Status != PayrollRoleStatus.Generated)
            throw new InvalidOperationException("El rol está autorizado/pagado. No se permiten cambios.");

        var adjustment = new PayrollAdjustment
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            Type = request.Type,
            Category = request.Category,
            Hours = request.Hours,
            Amount = request.Amount,
            Notes = request.Notes
        };

        _context.PayrollAdjustments.Add(adjustment);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PayrollAdjustmentDto>(adjustment);
    }
}

public class DeletePayrollAdjustmentCommandHandler : IRequestHandler<DeletePayrollAdjustmentCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeletePayrollAdjustmentCommandHandler(GrimorioDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeletePayrollAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await _context.PayrollAdjustments
            .FirstOrDefaultAsync(a => a.Id == request.PayrollAdjustmentId && a.BranchId == request.BranchId && !a.IsDeleted, cancellationToken);

        if (adjustment == null)
            throw new InvalidOperationException("Ajuste no encontrado.");

        var role = await _context.PayrollRoleHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BranchId == request.BranchId
                && r.EmployeeId == adjustment.EmployeeId
                && r.Year == adjustment.Date.Year
                && r.Month == adjustment.Date.Month
                && !r.IsDeleted, cancellationToken);

        if (role != null && role.Status != PayrollRoleStatus.Generated)
            throw new InvalidOperationException("El rol está autorizado/pagado. No se permiten cambios.");

        adjustment.IsDeleted = true;
        adjustment.DeletedAt = DateTime.UtcNow;

        _context.PayrollAdjustments.Update(adjustment);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class GenerateMonthlyPayrollRolesCommandHandler : IRequestHandler<GenerateMonthlyPayrollRolesCommand, GeneratePayrollRolesResultDto>
{
    private readonly GrimorioDbContext _context;

    public GenerateMonthlyPayrollRolesCommandHandler(GrimorioDbContext context)
    {
        _context = context;
    }

    public async Task<GeneratePayrollRolesResultDto> Handle(GenerateMonthlyPayrollRolesCommand request, CancellationToken cancellationToken)
    {
        var startDate = new DateTime(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var config = await _context.PayrollConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(pc => pc.BranchId == request.BranchId && !pc.IsDeleted, cancellationToken)
            ?? new PayrollConfiguration { BranchId = request.BranchId };

        var employeeQuery = _context.Employees
            .AsNoTracking()
            .Where(e => e.BranchId == request.BranchId && e.IsActive && !e.IsDeleted);

        // If specific employee requested, filter to only that employee
        if (request.EmployeeId.HasValue)
            employeeQuery = employeeQuery.Where(e => e.Id == request.EmployeeId.Value);

        var employees = await employeeQuery
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

        var generatedCount = 0;
        var updatedCount = 0;

        foreach (var employee in employees)
        {
            var payrollDetails = BuildPayrollDetails(employee, config, advancesByEmployee, consumptionsByEmployee, adjustmentsByEmployee);

            var totalIncome = payrollDetails.Where(d => d.Type == PayrollRoleDetailType.Income).Sum(d => d.Amount);
            var totalDeductions = payrollDetails.Where(d => d.Type == PayrollRoleDetailType.Deduction).Sum(d => d.Amount);
            var netPay = totalIncome - totalDeductions;

            // Check if exists WITHOUT tracking first
            var existsHeader = await _context.PayrollRoleHeaders
                .AsNoTracking()
                .Where(r => r.BranchId == request.BranchId 
                    && r.EmployeeId == employee.Id 
                    && r.Year == request.Year 
                    && r.Month == request.Month 
                    && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existsHeader == null)
            {
                var header = new PayrollRoleHeader
                {
                    Id = Guid.NewGuid(),
                    BranchId = request.BranchId,
                    EmployeeId = employee.Id,
                    Year = request.Year,
                    Month = request.Month,
                    Status = PayrollRoleStatus.Generated,
                    GeneratedAt = DateTime.UtcNow,
                    TotalIncome = totalIncome,
                    TotalDeductions = totalDeductions,
                    NetPay = netPay,
                    Details = payrollDetails
                };

                _context.PayrollRoleHeaders.Add(header);
                generatedCount++;
            }
            else
            {
                // Delete old details directly using SQL
                await _context.PayrollRoleDetails
                    .Where(d => d.PayrollRoleHeaderId == existsHeader.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                // Update header with new values
                await _context.PayrollRoleHeaders
                    .Where(r => r.Id == existsHeader.Id)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(r => r.TotalIncome, totalIncome)
                            .SetProperty(r => r.TotalDeductions, totalDeductions)
                            .SetProperty(r => r.NetPay, netPay),
                        cancellationToken);

                // Assign PayrollRoleHeaderId to new details and add them
                foreach (var detail in payrollDetails)
                {
                    detail.PayrollRoleHeaderId = existsHeader.Id;
                }
                _context.PayrollRoleDetails.AddRange(payrollDetails);
                updatedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new GeneratePayrollRolesResultDto
        {
            Year = request.Year,
            Month = request.Month,
            GeneratedCount = generatedCount,
            UpdatedCount = updatedCount,
        };
    }

    private static List<PayrollRoleDetail> BuildPayrollDetails(
        Domain.Entities.Organization.Employee employee,
        PayrollConfiguration config,
        Dictionary<Guid, decimal> advancesByEmployee,
        Dictionary<Guid, decimal> consumptionsByEmployee,
        Dictionary<Guid, List<PayrollAdjustment>> adjustmentsByEmployee)
    {
        var details = new List<PayrollRoleDetail>();
        var sort = 1;

        var baseSalary = employee.BaseSalary;
        var iessEmployee = CalculatePercent(baseSalary, config.IessEmployeeRate);
        var incomeTax = CalculatePercent(baseSalary, config.IncomeTaxRate);
        var decimoThird = employee.DecimoThirdMonthly ? CalculatePercent(baseSalary, config.DecimoThirdRate) : 0m;
        var decimoFourth = employee.DecimoFourthMonthly ? CalculatePercent(baseSalary, config.DecimoFourthRate) : 0m;
        var reserveFund = employee.ReserveFundMonthly ? CalculatePercent(baseSalary, config.ReserveFundRate) : 0m;

        var overtime50 = 0m;
        var overtime100 = 0m;
        var otherIncome = 0m;
        var otherDeductions = 0m;
        var adjustmentNotes = new StringBuilder();

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

                if (!string.IsNullOrEmpty(adjustment.Notes))
                {
                    if (adjustmentNotes.Length > 0)
                        adjustmentNotes.Append("; ");
                    adjustmentNotes.Append(adjustment.Notes);
                }
            }
        }

        var advances = advancesByEmployee.TryGetValue(employee.Id, out var adv) ? adv : 0m;
        var consumptions = consumptionsByEmployee.TryGetValue(employee.Id, out var cons) ? cons : 0m;

        AddDetail(details, PayrollRoleDetailType.Income, "Sueldo base", baseSalary, sort++, null);
        AddDetail(details, PayrollRoleDetailType.Income, "Décimo tercero", decimoThird, sort++, null);
        AddDetail(details, PayrollRoleDetailType.Income, "Décimo cuarto", decimoFourth, sort++, null);
        AddDetail(details, PayrollRoleDetailType.Income, "Fondo de reserva", reserveFund, sort++, null);
        AddDetail(details, PayrollRoleDetailType.Income, "Horas extra 50%", overtime50, sort++, null);
        AddDetail(details, PayrollRoleDetailType.Income, "Horas extra 100%", overtime100, sort++, null);
        AddDetail(details, PayrollRoleDetailType.Income, "Otros ingresos", otherIncome, sort++, null);

        AddDetail(details, PayrollRoleDetailType.Deduction, "IESS empleado", iessEmployee, sort++, null);
        AddDetail(details, PayrollRoleDetailType.Deduction, "Impuesto renta", incomeTax, sort++, null);
        AddDetail(details, PayrollRoleDetailType.Deduction, "Anticipos", advances, sort++, advances > 0 ? "Incluye anticipos del período" : null);
        AddDetail(details, PayrollRoleDetailType.Deduction, "Consumos", consumptions, sort++, consumptions > 0 ? "Incluye consumos del período" : null);
        AddDetail(details, PayrollRoleDetailType.Deduction, "Otros descuentos", otherDeductions, sort++, adjustmentNotes.Length > 0 ? adjustmentNotes.ToString() : null);

        return details.Where(d => d.Amount != 0m).ToList();
    }

    private static void AddDetail(List<PayrollRoleDetail> details, PayrollRoleDetailType type, string concept, decimal amount, int sortOrder, string? notes)
    {
        details.Add(new PayrollRoleDetail
        {
            Id = Guid.NewGuid(),
            Type = type,
            Concept = concept,
            Amount = amount,
            SortOrder = sortOrder,
            Notes = notes
        });
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

public class UpdatePayrollRoleStatusCommandHandler : IRequestHandler<UpdatePayrollRoleStatusCommand, PayrollRoleDto>
{
    private readonly GrimorioDbContext _context;

    public UpdatePayrollRoleStatusCommandHandler(GrimorioDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollRoleDto> Handle(UpdatePayrollRoleStatusCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.PayrollRoleHeaders
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRoleId && r.BranchId == request.BranchId && !r.IsDeleted, cancellationToken);

        if (role == null)
            throw new InvalidOperationException("Rol de pago no encontrado.");

        role.Status = request.Status;
        if (request.Status == PayrollRoleStatus.Authorized)
            role.AuthorizedAt = DateTime.UtcNow;
        if (request.Status == PayrollRoleStatus.Paid)
            role.PaidAt = DateTime.UtcNow;

        _context.PayrollRoleHeaders.Update(role);
        await _context.SaveChangesAsync(cancellationToken);

        return new PayrollRoleDto
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
            NetPay = role.NetPay,
        };
    }
}
