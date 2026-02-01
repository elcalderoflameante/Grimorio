using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Organization;

/// <summary>
/// Entidad de rama (sucursal del negocio en Ecuador).
/// Cada sucursal es aislada en el mismo DB usando BranchId.
/// </summary>
public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Ubicación geográfica
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Relaciones
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

/// <summary>
/// Entidad de cargo/puesto de trabajo.
/// Define los cargos disponibles en la empresa (e.g., Chef, Mesero, Cajero).
/// </summary>
public class Position : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Relación
    public Guid BranchIdParent { get; set; } // BranchId donde se define el cargo
    public Branch? Branch { get; set; }

    // Relación inversa
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

/// <summary>
/// Entidad de empleado.
/// Incluye datos personales, cargo, sucursal asignada, y estado.
/// </summary>
public class Employee : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty; // Cédula en Ecuador
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Relaciones
    public Guid PositionId { get; set; }
    public Position? Position { get; set; }

    public Guid BranchIdAssigned { get; set; } // Rama (sucursal) donde trabaja
    public Branch? BranchAssigned { get; set; }

    // Relación inversa
    public ICollection<EmployeeShift> EmployeeShifts { get; set; } = new List<EmployeeShift>();
    public ICollection<EmployeeClocking> EmployeeClockings { get; set; } = new List<EmployeeClocking>();
}

/// <summary>
/// Entidad de turno de empleado.
/// Define horarios de trabajo (mañana, tarde, noche, etc.).
/// </summary>
public class EmployeeShift : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public string ShiftName { get; set; } = string.Empty; // e.g., "Morning", "Afternoon", "Night"
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    // Relación
    public Employee? Employee { get; set; }
}

/// <summary>
/// Entidad de marcaje (clock-in/clock-out).
/// Registra entrada y salida de empleados.
/// </summary>
public class EmployeeClocking : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public string? Notes { get; set; }
    public bool IsLate { get; set; } = false;
    public TimeSpan? LateMinutes { get; set; }

    // Relación
    public Employee? Employee { get; set; }
}
