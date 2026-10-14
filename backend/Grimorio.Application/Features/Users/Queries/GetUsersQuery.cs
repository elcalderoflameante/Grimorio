using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Users.Queries;

public class GetUsersQuery : IRequest<List<UserDto>>
{
    public Guid BranchId { get; set; }
}
