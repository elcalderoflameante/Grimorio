using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Users.Commands;
using Grimorio.Application.Features.Users.Queries;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetUsersQuery()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _mediator.Send(new GetUserByIdQuery { UserId = id }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        return Ok(await _mediator.Send(new CreateUserCommand { Dto = dto, BranchId = branchId }));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto) => Ok(await _mediator.Send(new UpdateUserCommand { UserId = id, Dto = dto }));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id) => Ok(await _mediator.Send(new DeleteUserCommand { UserId = id }));

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesDto dto)
    {
        try
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
                return Unauthorized("BranchId no válido en el token.");

            // Convertir strings a GUIDs
            var roleIds = new List<Guid>();
            foreach (var roleIdStr in dto.RoleIds)
            {
                if (!Guid.TryParse(roleIdStr, out var roleId))
                {
                    return BadRequest($"ID de rol inválido: {roleIdStr}");
                }
                roleIds.Add(roleId);
            }

            return Ok(await _mediator.Send(new AssignRolesToUserCommand { UserId = id, RoleIds = roleIds, BranchId = branchId }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UsersController.AssignRoles Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw;
        }
    }
}
