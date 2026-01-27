using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Permissions.Commands;

public class CreatePermissionCommand : IRequest<PermissionDto>
{
    public CreatePermissionDto Dto { get; set; } = new();
}
