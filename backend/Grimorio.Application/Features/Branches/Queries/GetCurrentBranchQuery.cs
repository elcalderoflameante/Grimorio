using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Branches.Queries;

/// <summary>
/// Query para obtener la sucursal actual.
/// </summary>
public class GetCurrentBranchQuery : IRequest<BranchDto?>
{
    public Guid BranchId { get; set; }
}
