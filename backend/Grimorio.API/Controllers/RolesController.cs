using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Roles.Commands;
using Grimorio.Application.Features.Roles.Queries;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    public RolesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetRolesQuery()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _mediator.Send(new GetRoleByIdQuery { RoleId = id }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto) => Ok(await _mediator.Send(new CreateRoleCommand { Dto = dto }));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleDto dto) => Ok(await _mediator.Send(new UpdateRoleCommand { RoleId = id, Dto = dto }));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id) => Ok(await _mediator.Send(new DeleteRoleCommand { RoleId = id }));

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignPermissionsDto dto) => Ok(await _mediator.Send(new AssignPermissionsToRoleCommand { RoleId = id, PermissionIds = dto.PermissionIds }));
}
