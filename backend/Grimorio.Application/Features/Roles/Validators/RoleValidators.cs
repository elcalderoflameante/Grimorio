using FluentValidation;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Roles.Commands;
using Grimorio.Application.Features.Roles.Queries;

namespace Grimorio.Application.Features.Roles.Validators;

public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
{
    public CreateRoleDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateRoleDtoValidator : AbstractValidator<UpdateRoleDto>
{
    public UpdateRoleDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Dto).NotNull().SetValidator(new CreateRoleDtoValidator());
    }
}

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Dto).NotNull().SetValidator(new UpdateRoleDtoValidator());
    }
}

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public class AssignPermissionsToRoleCommandValidator : AbstractValidator<AssignPermissionsToRoleCommand>
{
    public AssignPermissionsToRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionIds).NotEmpty();
        RuleForEach(x => x.PermissionIds).NotEmpty();
    }
}

public class GetRolesQueryValidator : AbstractValidator<GetRolesQuery>
{
    public GetRolesQueryValidator() { }
}

public class GetRoleByIdQueryValidator : AbstractValidator<GetRoleByIdQuery>
{
    public GetRoleByIdQueryValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
