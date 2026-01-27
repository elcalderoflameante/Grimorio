using MediatR;

namespace Grimorio.Application.Features.Permissions.Commands;

public class DeletePermissionCommand : IRequest<bool>
{
    public Guid PermissionId { get; set; }
}
