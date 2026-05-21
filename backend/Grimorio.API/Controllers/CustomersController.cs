using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomersController(IMediator mediator) => _mediator = mediator;

    [Authorize(Policy = "Billing.Customers.View")]
    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] bool? activeOnly, [FromQuery] string? search)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetCustomersQuery { BranchId = branchId, ActiveOnly = activeOnly, Search = search }));
    }

    [Authorize(Policy = "Billing.Customers.Manage")]
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateCustomerCommand
        {
            BranchId = branchId, Name = dto.Name, TaxId = dto.TaxId,
            TaxIdType = dto.TaxIdType, Address = dto.Address,
            Phone = dto.Phone, Email = dto.Email,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Customers.Manage")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateCustomerCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name, TaxId = dto.TaxId,
            TaxIdType = dto.TaxIdType, Address = dto.Address,
            Phone = dto.Phone, Email = dto.Email, IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Customers.Manage")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteCustomerCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId);
    }
}
