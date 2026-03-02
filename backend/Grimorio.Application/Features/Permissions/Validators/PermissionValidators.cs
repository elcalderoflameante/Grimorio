using FluentValidation;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Permissions.Commands;
using Grimorio.Application.Features.Permissions.Queries;

namespace Grimorio.Application.Features.Permissions.Validators;

public class CreatePermissionDtoValidator : AbstractValidator<CreatePermissionDto>
{
    public CreatePermissionDtoValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
    }
}

public class UpdatePermissionDtoValidator : AbstractValidator<UpdatePermissionDto>
{
    public UpdatePermissionDtoValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
    }
}

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.Dto).NotNull().SetValidator(new CreatePermissionDtoValidator());
    }
}

public class UpdatePermissionCommandValidator : AbstractValidator<UpdatePermissionCommand>
{
    public UpdatePermissionCommandValidator()
    {
        RuleFor(x => x.PermissionId).NotEmpty();
        RuleFor(x => x.Dto).NotNull().SetValidator(new UpdatePermissionDtoValidator());
    }
}

public class DeletePermissionCommandValidator : AbstractValidator<DeletePermissionCommand>
{
    public DeletePermissionCommandValidator()
    {
        RuleFor(x => x.PermissionId).NotEmpty();
    }
}

public class GetPermissionsQueryValidator : AbstractValidator<GetPermissionsQuery>
{
    public GetPermissionsQueryValidator() { }
}

public class GetPermissionByIdQueryValidator : AbstractValidator<GetPermissionByIdQuery>
{
    public GetPermissionByIdQueryValidator()
    {
        RuleFor(x => x.PermissionId).NotEmpty();
    }
}
