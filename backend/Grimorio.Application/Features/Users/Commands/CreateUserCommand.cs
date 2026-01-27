using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Users.Commands;

public class CreateUserCommand : IRequest<UserDto>
{
    public CreateUserDto Dto { get; set; } = new();
    public Guid BranchId { get; set; }
}
