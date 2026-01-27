namespace Grimorio.SharedKernel;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// Incluye campos de auditoría, soft delete y GUID como identificador único.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Identificador único de la entidad (GUID/UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identificador de la rama (sucursal) a la que pertenece esta entidad.
    /// Permite aislar datos por rama del negocio en Ecuador.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Timestamp de creación de la entidad (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID del usuario que creó la entidad.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Timestamp de la última actualización (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// ID del usuario que realizó la última actualización.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Indica si la entidad ha sido eliminada (soft delete).
    /// Una entidad eliminada no debe aparecer en consultas normales.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp de eliminación (soft delete).
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// ID del usuario que eliminó la entidad.
    /// </summary>
    public Guid? DeletedBy { get; set; }
}
