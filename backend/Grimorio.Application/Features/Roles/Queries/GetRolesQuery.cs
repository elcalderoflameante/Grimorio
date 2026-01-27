using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Roles.Queries;

public class GetRolesQuery : IRequest<List<RoleDto>> {}
