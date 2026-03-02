using FluentValidation;
using Grimorio.Application.Features.Auth.Commands;

namespace Grimorio.Application.Features.Auth.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
