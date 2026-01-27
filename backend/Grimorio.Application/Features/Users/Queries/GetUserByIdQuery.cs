using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Users.Queries;

public class GetUserByIdQuery : IRequest<UserDto>
{
    public Guid UserId { get; set; }
}
