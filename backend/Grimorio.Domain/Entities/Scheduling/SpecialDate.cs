using System;
using System.Collections.Generic;
using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Scheduling;

/// <summary>
/// Día especial: fecha específica (Valentine, Carnaval, etc) que requiere plantilla diferente
/// </summary>
public class SpecialDate : BaseEntity
{
    public DateTime Date { get; set; } // La fecha específica (ej: 2026-02-14)
    public string Name { get; set; } = string.Empty; // Nombre del día especial (Valentine, Carnaval, etc)
    public string? Description { get; set; }
    
    // Navegación a sus plantillas específicas
    public virtual ICollection<SpecialDateTemplate> Templates { get; set; } = new List<SpecialDateTemplate>();
}

/// <summary>
/// Plantilla específica para un día especial
/// Similar a ShiftTemplate pero vinculada a una fecha específica
/// </summary>
public class SpecialDateTemplate : BaseEntity
{
    public Guid SpecialDateId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid WorkRoleId { get; set; }
    public int RequiredCount { get; set; } = 1;
    public string? Notes { get; set; }
    
    // Navegación
    public virtual SpecialDate? SpecialDate { get; set; }
    public virtual WorkArea? WorkArea { get; set; }
    public virtual WorkRole? WorkRole { get; set; }
}
