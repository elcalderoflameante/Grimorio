using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Permissions.Commands;

public class UpdatePermissionCommand : IRequest<PermissionDto>
{
    public Guid PermissionId { get; set; }
    public UpdatePermissionDto Dto { get; set; } = new();
}
