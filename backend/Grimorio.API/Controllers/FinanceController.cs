using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Finance.Commands;
using Grimorio.Application.Features.Finance.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/finance")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinanceController(IMediator mediator) => _mediator = mediator;

    [Authorize(Policy = "Finance.Config.View")]
    [HttpGet("cost-centers")]
    public async Task<IActionResult> GetCostCenters([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetCostCentersQuery { BranchId = branchId, ActiveOnly = activeOnly }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Config.Manage")]
    [HttpPost("cost-centers")]
    public async Task<IActionResult> CreateCostCenter([FromBody] UpsertCostCenterDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new CreateCostCenterCommand
        {
            BranchId = branchId,
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Config.Manage")]
    [HttpPut("cost-centers/{id:guid}")]
    public async Task<IActionResult> UpdateCostCenter(Guid id, [FromBody] UpsertCostCenterDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new UpdateCostCenterCommand
        {
            Id = id,
            BranchId = branchId,
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Config.Manage")]
    [HttpDelete("cost-centers/{id:guid}")]
    public async Task<IActionResult> DeleteCostCenter(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        await _mediator.Send(new DeleteCostCenterCommand { Id = id, BranchId = branchId }, ct);
        return NoContent();
    }

    [Authorize(Policy = "Finance.Config.View")]
    [HttpGet("expense-categories")]
    public async Task<IActionResult> GetExpenseCategories(
        [FromQuery] bool? activeOnly,
        [FromQuery] string? type,
        CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetExpenseCategoriesQuery
        {
            BranchId = branchId,
            ActiveOnly = activeOnly,
            Type = type,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Config.Manage")]
    [HttpPost("expense-categories")]
    public async Task<IActionResult> CreateExpenseCategory([FromBody] UpsertExpenseCategoryDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new CreateExpenseCategoryCommand
        {
            BranchId = branchId,
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Config.Manage")]
    [HttpPut("expense-categories/{id:guid}")]
    public async Task<IActionResult> UpdateExpenseCategory(Guid id, [FromBody] UpsertExpenseCategoryDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new UpdateExpenseCategoryCommand
        {
            Id = id,
            BranchId = branchId,
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Config.Manage")]
    [HttpDelete("expense-categories/{id:guid}")]
    public async Task<IActionResult> DeleteExpenseCategory(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        await _mediator.Send(new DeleteExpenseCategoryCommand { Id = id, BranchId = branchId }, ct);
        return NoContent();
    }

    [Authorize(Policy = "Finance.Expenses.View")]
    [HttpGet("expenses")]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] string? status,
        [FromQuery] Guid? costCenterId,
        [FromQuery] Guid? expenseCategoryId,
        [FromQuery] Guid? cashSessionId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetExpensesQuery
        {
            BranchId = branchId,
            Status = status,
            CostCenterId = costCenterId,
            ExpenseCategoryId = expenseCategoryId,
            CashSessionId = cashSessionId,
            FromUtc = from,
            ToUtc = to,
            PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Expenses.View")]
    [HttpGet("expenses/report")]
    public async Task<IActionResult> GetExpenseReport(
        [FromQuery] Guid? costCenterId,
        [FromQuery] Guid? expenseCategoryId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetExpenseReportQuery
        {
            BranchId = branchId,
            CostCenterId = costCenterId,
            ExpenseCategoryId = expenseCategoryId,
            FromUtc = from,
            ToUtc = to,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Expenses.View")]
    [HttpGet("income-statement")]
    public async Task<IActionResult> GetIncomeStatement(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetIncomeStatementQuery
        {
            BranchId = branchId,
            FromUtc = from,
            ToUtc = to,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Expenses.View")]
    [HttpGet("cost-center-profitability")]
    public async Task<IActionResult> GetCostCenterProfitability(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetCostCenterProfitabilityQuery
        {
            BranchId = branchId,
            FromUtc = from,
            ToUtc = to,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Expenses.Create")]
    [HttpPost("expenses")]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");
        if (!TryGetUserId(out var userId)) return Unauthorized("UserId no valido en el token.");

        var result = await _mediator.Send(new CreateExpenseCommand
        {
            BranchId = branchId,
            UserId = userId,
            UserName = BuildUserName(),
            ExpenseDate = dto.ExpenseDate,
            CostCenterId = dto.CostCenterId,
            ExpenseCategoryId = dto.ExpenseCategoryId,
            PaymentMethodConfigId = dto.PaymentMethodConfigId,
            CashSessionId = dto.CashSessionId,
            SupplierName = dto.SupplierName,
            DocumentNumber = dto.DocumentNumber,
            Amount = dto.Amount,
            Notes = dto.Notes,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Finance.Expenses.Cancel")]
    [HttpPost("expenses/{id:guid}/cancel")]
    public async Task<IActionResult> CancelExpense(Guid id, [FromBody] CancelExpenseDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");
        if (!TryGetUserId(out var userId)) return Unauthorized("UserId no valido en el token.");

        var result = await _mediator.Send(new CancelExpenseCommand
        {
            Id = id,
            BranchId = branchId,
            UserId = userId,
            UserName = BuildUserName(),
            Reason = dto.Reason,
        }, ct);
        return Ok(result);
    }

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId) && branchId != Guid.Empty;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out userId) && userId != Guid.Empty;
    }

    private string BuildUserName()
    {
        var firstName = User.FindFirst(AppConstants.Claims.FirstName)?.Value ?? string.Empty;
        var lastName = User.FindFirst(AppConstants.Claims.LastName)?.Value ?? string.Empty;
        var full = $"{firstName} {lastName}".Trim();
        return string.IsNullOrEmpty(full) ? "Usuario" : full;
    }
}
