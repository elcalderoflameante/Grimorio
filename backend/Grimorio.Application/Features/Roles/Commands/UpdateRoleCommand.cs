using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Roles.Commands;

public class UpdateRoleCommand : IRequest<RoleDto>
{
    public Guid RoleId { get; set; }
    public UpdateRoleDto Dto { get; set; } = new();
}
