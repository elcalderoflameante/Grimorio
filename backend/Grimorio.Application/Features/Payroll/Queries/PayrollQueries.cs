using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Payroll.Queries;

public class GetPayrollConfigurationQuery : IRequest<PayrollConfigurationDto?>
{
    public Guid BranchId { get; set; }
}

public class GetPayrollSummaryQuery : IRequest<List<EmployeePayrollSummaryDto>>
{
    public Guid BranchId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

public class GetPayrollAdvancesQuery : IRequest<List<PayrollAdvanceDto>>
{
    public Guid BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}

public class GetEmployeeConsumptionsQuery : IRequest<List<EmployeeConsumptionDto>>
{
    public Guid BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}

public class GetPayrollAdjustmentsQuery : IRequest<List<PayrollAdjustmentDto>>
{
    public Guid BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}

public class GetPayrollRolesByEmployeeQuery : IRequest<List<PayrollRoleDto>>
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
}

public class GetPayrollRoleDetailQuery : IRequest<PayrollRoleFullDto?>
{
    public Guid BranchId { get; set; }
    public Guid PayrollRoleId { get; set; }
}
