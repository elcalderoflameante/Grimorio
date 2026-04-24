using MediatR;

namespace Grimorio.Application.Features.TableService.Commands;

public class RegisterPushTokenCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public string? DeviceId { get; set; }
}

public class DeactivatePushTokenCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}
