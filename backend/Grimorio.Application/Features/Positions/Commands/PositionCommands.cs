using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Positions.Commands;

/// <summary>
/// Comando para crear una nueva posición.
/// </summary>
public class CreatePositionCommand : IRequest<PositionDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// Comando para actualizar una posición existente.
/// </summary>
public class UpdatePositionCommand : IRequest<PositionDto>
{
    public Guid PositionId { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Comando para eliminar (soft delete) una posición.
/// </summary>
public class DeletePositionCommand : IRequest<bool>
{
    public Guid PositionId { get; set; }
    public Guid BranchId { get; set; }
}
