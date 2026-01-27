using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Roles.Queries;

public class GetRoleByIdQuery : IRequest<RoleDto>
{
    public Guid RoleId { get; set; }
}
