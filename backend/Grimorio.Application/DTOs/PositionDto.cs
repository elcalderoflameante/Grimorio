namespace Grimorio.Application.DTOs;

/// <summary>
/// DTO para lectura de posiciones.
/// </summary>
public class PositionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid BranchId { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO para crear una nueva posición.
/// </summary>
public class CreatePositionDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// DTO para actualizar una posición existente.
/// </summary>
public class UpdatePositionDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
}
