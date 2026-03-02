using FluentValidation;
using Grimorio.Application.Features.Branches.Commands;
using Grimorio.Application.Features.Branches.Queries;

namespace Grimorio.Application.Features.Branches.Validators;

public class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.IdentificationNumber).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
    }
}

public class GetCurrentBranchQueryValidator : AbstractValidator<GetCurrentBranchQuery>
{
    public GetCurrentBranchQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
    }
}
