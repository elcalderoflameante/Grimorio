using MediatR;
using FluentValidation;

namespace Grimorio.Application.Features.Users.Commands;

public class ResetUserPasswordCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ActorUserId { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
}

public class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(8)
            .Must(value => System.Text.Encoding.UTF8.GetByteCount(value ?? "") <= 72)
            .WithMessage("La contrasena debe ocupar como maximo 72 bytes UTF-8.");
    }
}
