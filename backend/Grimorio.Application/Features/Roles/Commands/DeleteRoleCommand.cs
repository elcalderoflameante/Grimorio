using MediatR;

namespace Grimorio.Application.Features.Roles.Commands;

public class DeleteRoleCommand : IRequest<bool>
{
    public Guid RoleId { get; set; }
}
