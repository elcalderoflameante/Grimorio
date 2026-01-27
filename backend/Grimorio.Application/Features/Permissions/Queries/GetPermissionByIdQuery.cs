using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Permissions.Queries;

public class GetPermissionByIdQuery : IRequest<PermissionDto>
{
    public Guid PermissionId { get; set; }
}
