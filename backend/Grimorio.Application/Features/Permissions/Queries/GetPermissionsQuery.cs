using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Permissions.Queries;

public class GetPermissionsQuery : IRequest<List<PermissionDto>> {}
