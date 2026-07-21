using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Auth.Queries;

public class GetKdsBranchesQuery : IRequest<List<KdsBranchDto>> { }

public class GetKdsUsersQuery : IRequest<List<KdsUserDto>>
{
    public Guid BranchId { get; set; }
}
