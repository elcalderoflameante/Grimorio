using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Positions.Queries;

/// <summary>
/// Query para obtener una posición por su ID.
/// </summary>
public class GetPositionQuery : IRequest<PositionDto>
{
    public Guid PositionId { get; set; }
    public Guid BranchId { get; set; }
}

/// <summary>
/// Query para obtener todas las posiciones de una rama con paginación.
/// </summary>
public class GetPositionsQuery : IRequest<PaginatedPositionsResult>
{
    public Guid BranchId { get; set; }
    public bool OnlyActive { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Resultado paginado de posiciones.
/// </summary>
public class PaginatedPositionsResult
{
    public List<PositionDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
