using MediatR;

namespace Grimorio.Application.Features.Users.Commands;

public class SetKdsPinCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string? Pin { get; set; }
}
