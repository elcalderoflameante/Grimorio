using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Users.Commands;

public class UpdateUserCommand : IRequest<UserDto>
{
    public Guid UserId { get; set; }
    public UpdateUserDto Dto { get; set; } = new();
}
