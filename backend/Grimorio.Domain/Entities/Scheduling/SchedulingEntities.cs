namespace Grimorio.Domain.Entities.Scheduling;

/// <summary>
/// Área de trabajo en el restaurante (Caja, Cocina, Bar, Mesas)
/// </summary>
public class WorkArea : Grimorio.SharedKernel.BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#808080"; // Color para UI (hex)
    public int DisplayOrder { get; set; }
    public Guid BranchId { get; set; }
    
    // Navegación
    public virtual ICollection<WorkRole> WorkRoles { get; set; } = new List<WorkRole>();
    public virtual ICollection<ShiftTemplate> ShiftTemplates { get; set; } = new List<ShiftTemplate>();
}

/// <summary>
/// Rol dentro de un área (Parrillero, Ayudante, Mesera, etc.)
/// </summary>
public class WorkRole : Grimorio.SharedKernel.BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid WorkAreaId { get; set; }
    public int FreeDaysPerMonth { get; set; } = 6; // Días libres por defecto
    public decimal DailyHoursTarget { get; set; } = 8.0m; // Horas objetivo por día
    
    // Navegación
    public virtual WorkArea? WorkArea { get; set; }
    public virtual ICollection<EmployeeWorkRole> EmployeeWorkRoles { get; set; } = new List<EmployeeWorkRole>();
}

/// <summary>
/// Roles que puede desempeñar un empleado (muchos a muchos)
/// </summary>
public class EmployeeWorkRole : Grimorio.SharedKernel.BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid WorkRoleId { get; set; }
    public bool IsPrimary { get; set; } // Rol principal del empleado
    public int Priority { get; set; } = 1; // Prioridad para asignación (1=más alto)
    
    // Navegación
    public virtual Organization.Employee? Employee { get; set; }
    public virtual WorkRole? WorkRole { get; set; }
}

/// <summary>
/// Plantilla de turno: define necesidades por día de semana y franja horaria
/// </summary>
public class ShiftTemplate : Grimorio.SharedKernel.BaseEntity
{
    public Guid BranchId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; } // 30 min descanso
    public TimeSpan? LunchDuration { get; set; } // 1 hora almuerzo fines de semana
    public Guid WorkAreaId { get; set; }
    public Guid WorkRoleId { get; set; }
    public int RequiredCount { get; set; } = 1; // Cantidad de personas necesarias
    public string? Notes { get; set; }
    
    // Navegación
    public virtual WorkArea? WorkArea { get; set; }
    public virtual WorkRole? WorkRole { get; set; }
}

/// <summary>
/// Asignación concreta de turno a un empleado en una fecha específica
/// </summary>
public class ShiftAssignment : Grimorio.SharedKernel.BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid WorkRoleId { get; set; }
    public decimal WorkedHours { get; set; } // Horas netas trabajadas
    public string? Notes { get; set; }
    public bool IsApproved { get; set; } = false;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    // Navegación
    public virtual Organization.Employee? Employee { get; set; }
    public virtual WorkArea? WorkArea { get; set; }
    public virtual WorkRole? WorkRole { get; set; }
}

/// <summary>
/// Disponibilidad del empleado: días que NO puede trabajar
/// </summary>
public class EmployeeAvailability : Grimorio.SharedKernel.BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime UnavailableDate { get; set; }
    public string? Reason { get; set; }
    
    // Navegación
    public virtual Organization.Employee? Employee { get; set; }
}
