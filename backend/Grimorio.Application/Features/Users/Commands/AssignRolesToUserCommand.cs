using MediatR;

namespace Grimorio.Application.Features.Users.Commands;

public class AssignRolesToUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public Guid BranchId { get; set; }
}
