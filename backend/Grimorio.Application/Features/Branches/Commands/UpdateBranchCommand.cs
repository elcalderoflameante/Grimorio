using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Branches.Commands;

/// <summary>
/// Comando para actualizar datos de la sucursal actual.
/// </summary>
public class UpdateBranchCommand : IRequest<BranchDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
