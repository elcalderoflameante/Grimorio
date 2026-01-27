using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Permissions.Commands;
using Grimorio.Application.Features.Permissions.Queries;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PermissionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetPermissionsQuery()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _mediator.Send(new GetPermissionByIdQuery { PermissionId = id }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePermissionDto dto) => Ok(await _mediator.Send(new CreatePermissionCommand { Dto = dto }));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionDto dto) => Ok(await _mediator.Send(new UpdatePermissionCommand { PermissionId = id, Dto = dto }));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id) => Ok(await _mediator.Send(new DeletePermissionCommand { PermissionId = id }));
}
