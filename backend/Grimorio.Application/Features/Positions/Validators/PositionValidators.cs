using FluentValidation;
using Grimorio.Application.Features.Positions.Commands;
using Grimorio.Application.Features.Positions.Queries;

namespace Grimorio.Application.Features.Positions.Validators;

public class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionCommandValidator()
    {
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class DeletePositionCommandValidator : AbstractValidator<DeletePositionCommand>
{
    public DeletePositionCommandValidator()
    {
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetPositionQueryValidator : AbstractValidator<GetPositionQuery>
{
    public GetPositionQueryValidator()
    {
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetPositionsQueryValidator : AbstractValidator<GetPositionsQuery>
{
    public GetPositionsQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
