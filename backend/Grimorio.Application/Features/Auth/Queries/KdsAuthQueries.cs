using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Auth.Queries;

public class GetKdsBranchesQuery : IRequest<List<KdsBranchDto>> { }
public record GetInventoryUsersQuery(Guid BranchId) : IRequest<List<KdsUserDto>>;

public class GetKdsUsersQuery : IRequest<List<KdsUserDto>>
{
    public Guid BranchId { get; set; }
}

public class GetWaitstaffUsersQuery : IRequest<List<KdsUserDto>>
{
    public Guid BranchId { get; set; }
}
