using MediatR;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Enums;

namespace Grimorio.Application.Features.Employees.Commands;

/// <summary>
/// Comando para crear un nuevo empleado.
/// </summary>
public class CreateEmployeeCommand : IRequest<EmployeeDto>
{
    public Guid BranchId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public Guid PositionId { get; set; }
    public DateTime HireDate { get; set; }
    public ContractType ContractType { get; set; }
    public decimal WeeklyMinHours { get; set; }
    public decimal WeeklyMaxHours { get; set; }
    public int FreeDaysPerMonth { get; set; } = 6;
}

/// <summary>
/// Comando para actualizar un empleado existente.
/// </summary>
public class UpdateEmployeeCommand : IRequest<EmployeeDto>
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public Guid PositionId { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsActive { get; set; }
    public ContractType ContractType { get; set; }
    public decimal WeeklyMinHours { get; set; }
    public decimal WeeklyMaxHours { get; set; }
    public int FreeDaysPerMonth { get; set; }
}

/// <summary>
/// Comando para eliminar (soft delete) un empleado.
/// </summary>
public class DeleteEmployeeCommand : IRequest<bool>
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
}
