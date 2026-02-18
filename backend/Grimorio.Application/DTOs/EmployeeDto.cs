using Grimorio.Domain.Enums;

namespace Grimorio.Application.DTOs;

/// <summary>
/// DTO para mostrar información de un empleado.
/// </summary>
public class EmployeeDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public Guid PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsActive { get; set; }
    public ContractType ContractType { get; set; }
    public decimal WeeklyMinHours { get; set; }
    public decimal WeeklyMaxHours { get; set; }
    public int FreeDaysPerMonth { get; set; }
}

/// <summary>
/// DTO para crear un nuevo empleado.
/// </summary>
public class CreateEmployeeDto
{
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
/// DTO para actualizar un empleado existente.
/// </summary>
public class UpdateEmployeeDto
{
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
