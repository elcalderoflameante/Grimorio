using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Users.Queries;

public class GetUserEmployeeOptionsQuery : IRequest<List<UserEmployeeOptionDto>>
{
    public Guid BranchId { get; set; }
}
