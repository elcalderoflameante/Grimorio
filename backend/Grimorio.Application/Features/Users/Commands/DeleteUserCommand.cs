using MediatR;

namespace Grimorio.Application.Features.Users.Commands;

public class DeleteUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
}
