namespace Grimorio.Application.DTOs;

/// <summary>
/// DTO para Área de Trabajo (Caja, Cocina, Bar, Mesas)
/// </summary>
public class WorkAreaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#808080";
    public int DisplayOrder { get; set; }
    public Guid BranchId { get; set; }
    public List<WorkRoleDto> WorkRoles { get; set; } = new();
}

/// <summary>
/// DTO para Rol dentro de un Área (Parrillero, Ayudante de Cocina, etc.)
/// </summary>
public class WorkRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid WorkAreaId { get; set; }
    public int FreeDaysPerMonth { get; set; } = 6;
    public decimal DailyHoursTarget { get; set; } = 8.0m;
}

/// <summary>
/// DTO para Roles de Empleado (relación muchos a muchos)
/// </summary>
public class EmployeeWorkRoleDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid WorkRoleId { get; set; }
    public string WorkRoleName { get; set; } = string.Empty;
    public string WorkAreaName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// DTO para Plantilla de Turno (configuración semanal)
/// </summary>
public class ShiftTemplateDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public Guid WorkAreaId { get; set; }
    public string WorkAreaName { get; set; } = string.Empty;
    public Guid WorkRoleId { get; set; }
    public string WorkRoleName { get; set; } = string.Empty;
    public int RequiredCount { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO para Asignación de Turno (asignación concreta a un empleado)
/// </summary>
public class ShiftAssignmentDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public Guid WorkAreaId { get; set; }
    public string WorkAreaName { get; set; } = string.Empty;
    public Guid WorkRoleId { get; set; }
    public string WorkRoleName { get; set; } = string.Empty;
    public decimal WorkedHours { get; set; }
    public string? Notes { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

/// <summary>
/// DTO para Disponibilidad de Empleado (días no disponibles)
/// </summary>
public class EmployeeAvailabilityDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime UnavailableDate { get; set; }
    public string? Reason { get; set; }
}
