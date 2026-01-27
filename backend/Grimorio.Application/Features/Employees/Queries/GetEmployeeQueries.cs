using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Employees.Queries;

/// <summary>
/// Obtiene un empleado por su ID.
/// </summary>
public class GetEmployeeQuery : IRequest<EmployeeDto?>
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
}

/// <summary>
/// Obtiene todos los empleados de una rama.
/// </summary>
public class GetEmployeesQuery : IRequest<List<EmployeeDto>>
{
    public Guid BranchId { get; set; }
    public bool OnlyActive { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
