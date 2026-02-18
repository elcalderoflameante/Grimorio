namespace Grimorio.Domain.Enums;

/// <summary>
/// Tipo de contrato del empleado.
/// Define la categoría de empleo (Tiempo completo, Tiempo parcial, etc.)
/// </summary>
public enum ContractType
{
    /// <summary>
    /// Empleado de tiempo completo
    /// </summary>
    FullTime = 1,

    /// <summary>
    /// Empleado de tiempo parcial
    /// </summary>
    PartTime = 2,

    /// <summary>
    /// Empleado temporal
    /// </summary>
    Temporary = 3,

    /// <summary>
    /// Empleado de temporada (específico para periodos de alto volumen)
    /// </summary>
    Seasonal = 4
}
