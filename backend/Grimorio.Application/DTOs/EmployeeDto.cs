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
    public decimal BaseSalary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public bool DecimoThirdMonthly { get; set; }
    public bool DecimoFourthMonthly { get; set; }
    public bool ReserveFundMonthly { get; set; }
    public int FreeDaysPerMonth { get; set; }
    
    // Información personal adicional
    public string? Photo { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string CivilStatus { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    
    // Información de contacto de emergencia
    public string EmergencyContactPerson { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
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
    public decimal BaseSalary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public bool DecimoThirdMonthly { get; set; }
    public bool DecimoFourthMonthly { get; set; }
    public bool ReserveFundMonthly { get; set; }
    public int FreeDaysPerMonth { get; set; } = 6;
    
    // Información personal adicional
    public string? Photo { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string CivilStatus { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    
    // Información de contacto de emergencia
    public string EmergencyContactPerson { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
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
    public decimal BaseSalary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public bool DecimoThirdMonthly { get; set; }
    public bool DecimoFourthMonthly { get; set; }
    public bool ReserveFundMonthly { get; set; }
    public int FreeDaysPerMonth { get; set; }
    
    // Información personal adicional
    public string? Photo { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string CivilStatus { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    
    // Información de contacto de emergencia
    public string EmergencyContactPerson { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
}
