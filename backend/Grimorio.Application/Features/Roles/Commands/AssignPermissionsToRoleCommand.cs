using MediatR;

namespace Grimorio.Application.Features.Roles.Commands;

public class AssignPermissionsToRoleCommand : IRequest<bool>
{
    public Guid BranchId { get; set; }
    public Guid RoleId { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}
