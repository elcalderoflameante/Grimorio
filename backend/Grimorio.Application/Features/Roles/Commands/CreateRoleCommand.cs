using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Roles.Commands;

public class CreateRoleCommand : IRequest<RoleDto>
{
    public CreateRoleDto Dto { get; set; } = new();
}
