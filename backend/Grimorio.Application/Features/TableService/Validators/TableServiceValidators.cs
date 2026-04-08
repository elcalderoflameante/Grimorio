using FluentValidation;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.Domain.Entities.POS;

namespace Grimorio.Application.Features.TableService.Validators;

public class CreateRestaurantTableCommandValidator : AbstractValidator<CreateRestaurantTableCommand>
{
    public CreateRestaurantTableCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Area).MaximumLength(120);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 30);
    }
}

public class UpdateRestaurantTableCommandValidator : AbstractValidator<UpdateRestaurantTableCommand>
{
    public UpdateRestaurantTableCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Area).MaximumLength(120);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 30);
    }
}

public class PublicCreateTableServiceRequestCommandValidator : AbstractValidator<PublicCreateTableServiceRequestCommand>
{
    public PublicCreateTableServiceRequestCommandValidator()
    {
        RuleFor(x => x.TableToken).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.CustomMessage)
            .MaximumLength(400)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomMessage));
        RuleFor(x => x)
            .Must(x => x.Type != TableServiceRequestType.Custom || !string.IsNullOrWhiteSpace(x.CustomMessage))
            .WithMessage("El mensaje es obligatorio para solicitudes personalizadas.");
    }
}

public class SetTableServiceRequestStatusCommandValidator : AbstractValidator<SetTableServiceRequestStatusCommand>
{
    public SetTableServiceRequestStatusCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class GetRestaurantTablesQueryValidator : AbstractValidator<GetRestaurantTablesQuery>
{
    public GetRestaurantTablesQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
    }
}
