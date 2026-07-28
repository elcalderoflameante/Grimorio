using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Finance.Commands;
using Grimorio.Domain.Entities.Finance;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Finance.Commands;

public class CreateCostCenterHandler : IRequestHandler<CreateCostCenterCommand, CostCenterDto>
{
    private readonly GrimorioDbContext _db;
    public CreateCostCenterHandler(GrimorioDbContext db) => _db = db;

    public async Task<CostCenterDto> Handle(CreateCostCenterCommand req, CancellationToken ct)
    {
        await FinanceValidator.EnsureCostCenterUnique(_db, req.BranchId, req.Name, req.Code, null, ct);

        var entity = new CostCenter
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            Name = req.Name.Trim(),
            Code = FinanceValidator.NormalizeOptional(req.Code),
            Description = FinanceValidator.NormalizeOptional(req.Description),
            DisplayOrder = req.DisplayOrder,
            IsActive = req.IsActive,
        };

        _db.CostCenters.Add(entity);
        await _db.SaveChangesAsync(ct);
        return FinanceMapper.MapCostCenter(entity);
    }
}

public class UpdateCostCenterHandler : IRequestHandler<UpdateCostCenterCommand, CostCenterDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateCostCenterHandler(GrimorioDbContext db) => _db = db;

    public async Task<CostCenterDto> Handle(UpdateCostCenterCommand req, CancellationToken ct)
    {
        var entity = await _db.CostCenters
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Cost center no encontrado.");

        await FinanceValidator.EnsureCostCenterUnique(_db, req.BranchId, req.Name, req.Code, req.Id, ct);

        entity.Name = req.Name.Trim();
        entity.Code = FinanceValidator.NormalizeOptional(req.Code);
        entity.Description = FinanceValidator.NormalizeOptional(req.Description);
        entity.DisplayOrder = req.DisplayOrder;
        entity.IsActive = req.IsActive;

        await _db.SaveChangesAsync(ct);
        return FinanceMapper.MapCostCenter(entity);
    }
}

public class DeleteCostCenterHandler : IRequestHandler<DeleteCostCenterCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteCostCenterHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteCostCenterCommand req, CancellationToken ct)
    {
        var entity = await _db.CostCenters
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Cost center no encontrado.");

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class CreateExpenseCategoryHandler : IRequestHandler<CreateExpenseCategoryCommand, ExpenseCategoryDto>
{
    private readonly GrimorioDbContext _db;
    public CreateExpenseCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<ExpenseCategoryDto> Handle(CreateExpenseCategoryCommand req, CancellationToken ct)
    {
        var type = FinanceValidator.ParseExpenseCategoryType(req.Type);
        await FinanceValidator.EnsureExpenseCategoryUnique(_db, req.BranchId, req.Name, null, ct);

        var entity = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            Name = req.Name.Trim(),
            Description = FinanceValidator.NormalizeOptional(req.Description),
            Type = type,
            DisplayOrder = req.DisplayOrder,
            IsActive = req.IsActive,
        };

        _db.ExpenseCategories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return FinanceMapper.MapExpenseCategory(entity);
    }
}

public class UpdateExpenseCategoryHandler : IRequestHandler<UpdateExpenseCategoryCommand, ExpenseCategoryDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateExpenseCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<ExpenseCategoryDto> Handle(UpdateExpenseCategoryCommand req, CancellationToken ct)
    {
        var entity = await _db.ExpenseCategories
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Expense category no encontrada.");

        var type = FinanceValidator.ParseExpenseCategoryType(req.Type);
        await FinanceValidator.EnsureExpenseCategoryUnique(_db, req.BranchId, req.Name, req.Id, ct);

        entity.Name = req.Name.Trim();
        entity.Description = FinanceValidator.NormalizeOptional(req.Description);
        entity.Type = type;
        entity.DisplayOrder = req.DisplayOrder;
        entity.IsActive = req.IsActive;

        await _db.SaveChangesAsync(ct);
        return FinanceMapper.MapExpenseCategory(entity);
    }
}

public class DeleteExpenseCategoryHandler : IRequestHandler<DeleteExpenseCategoryCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteExpenseCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteExpenseCategoryCommand req, CancellationToken ct)
    {
        var entity = await _db.ExpenseCategories
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Expense category no encontrada.");

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class CreateExpenseHandler : IRequestHandler<CreateExpenseCommand, ExpenseDto>
{
    private readonly GrimorioDbContext _db;
    public CreateExpenseHandler(GrimorioDbContext db) => _db = db;

    public async Task<ExpenseDto> Handle(CreateExpenseCommand req, CancellationToken ct)
    {
        if (req.Amount <= 0)
            throw new InvalidOperationException("El valor del gasto debe ser mayor a cero.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var costCenter = await _db.CostCenters
            .FirstOrDefaultAsync(x => x.Id == req.CostCenterId && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Centro de costo no encontrado.");
        if (!costCenter.IsActive) throw new InvalidOperationException("El centro de costo esta inactivo.");

        var category = await _db.ExpenseCategories
            .FirstOrDefaultAsync(x => x.Id == req.ExpenseCategoryId && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Categoria de gasto no encontrada.");
        if (!category.IsActive) throw new InvalidOperationException("La categoria de gasto esta inactiva.");

        PaymentMethodConfig? paymentMethod = null;
        if (req.PaymentMethodConfigId.HasValue)
        {
            paymentMethod = await _db.PaymentMethodConfigs
                .FirstOrDefaultAsync(x => x.Id == req.PaymentMethodConfigId.Value && !x.IsDeleted, ct)
                ?? throw new KeyNotFoundException("Medio de pago no encontrado.");
            if (!paymentMethod.IsActive) throw new InvalidOperationException("El medio de pago esta inactivo.");
        }

        CashSession? cashSession = null;
        if (req.CashSessionId.HasValue)
        {
            cashSession = await _db.CashSessions
                .FromSqlInterpolated($"""
                    SELECT * FROM billing."CashSessions"
                    WHERE "Id" = {req.CashSessionId.Value}
                        AND "BranchId" = {req.BranchId}
                        AND "IsDeleted" = false
                    FOR UPDATE
                    """)
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("Sesion de caja no encontrada.");

            if (cashSession.Status != CashSessionStatus.Open)
                throw new InvalidOperationException("Solo se pueden registrar gastos en una caja abierta.");
            if (cashSession.OpenedBy != req.UserId)
                throw new InvalidOperationException("Solo puedes registrar gastos en la caja abierta con tu usuario.");
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            ExpenseDate = req.ExpenseDate,
            CostCenterId = req.CostCenterId,
            ExpenseCategoryId = req.ExpenseCategoryId,
            PaymentMethodConfigId = req.PaymentMethodConfigId,
            CashSessionId = req.CashSessionId,
            SupplierName = FinanceValidator.NormalizeOptional(req.SupplierName),
            DocumentNumber = FinanceValidator.NormalizeOptional(req.DocumentNumber),
            Amount = Math.Round(req.Amount, 2),
            Notes = FinanceValidator.NormalizeOptional(req.Notes),
            Status = ExpenseStatus.Registered,
            RegisteredAt = DateTime.UtcNow,
            RegisteredBy = req.UserId,
            RegisteredByName = req.UserName,
            CostCenter = costCenter,
            ExpenseCategory = category,
            PaymentMethodConfig = paymentMethod,
            CashSession = cashSession,
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return FinanceMapper.MapExpense(expense);
    }
}

public class CancelExpenseHandler : IRequestHandler<CancelExpenseCommand, ExpenseDto>
{
    private readonly GrimorioDbContext _db;
    public CancelExpenseHandler(GrimorioDbContext db) => _db = db;

    public async Task<ExpenseDto> Handle(CancelExpenseCommand req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var expense = await _db.Expenses
            .Include(x => x.CostCenter)
            .Include(x => x.ExpenseCategory)
            .Include(x => x.PaymentMethodConfig)
            .Include(x => x.CashSession).ThenInclude(s => s!.CashRegister)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Gasto no encontrado.");

        if (expense.Status == ExpenseStatus.Cancelled)
            throw new InvalidOperationException("El gasto ya esta anulado.");

        if (expense.CashSessionId.HasValue)
        {
            var session = await _db.CashSessions
                .FromSqlInterpolated($"""
                    SELECT * FROM billing."CashSessions"
                    WHERE "Id" = {expense.CashSessionId.Value}
                        AND "BranchId" = {req.BranchId}
                        AND "IsDeleted" = false
                    FOR UPDATE
                    """)
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("Sesion de caja no encontrada.");

            if (session.Status != CashSessionStatus.Open)
                throw new InvalidOperationException("No se puede anular un gasto de una caja ya cerrada.");
            if (session.OpenedBy != req.UserId)
                throw new InvalidOperationException("Solo puedes anular gastos de la caja abierta con tu usuario.");
        }

        expense.Status = ExpenseStatus.Cancelled;
        expense.CancelledAt = DateTime.UtcNow;
        expense.CancelledBy = req.UserId;
        expense.CancelledByName = req.UserName;
        expense.CancellationReason = FinanceValidator.NormalizeOptional(req.Reason);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return FinanceMapper.MapExpense(expense);
    }
}

internal static class FinanceValidator
{
    internal static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    internal static ExpenseCategoryType ParseExpenseCategoryType(string value)
    {
        if (Enum.TryParse<ExpenseCategoryType>(value, true, out var type))
            return type;
        throw new InvalidOperationException("Tipo de categoria de gasto no valido.");
    }

    internal static async Task EnsureCostCenterUnique(
        GrimorioDbContext db, Guid branchId, string name, string? code, Guid? excludeId, CancellationToken ct)
    {
        var normalizedCode = NormalizeOptional(code);
        var exists = await db.CostCenters.AnyAsync(x =>
            x.BranchId == branchId &&
            (!excludeId.HasValue || x.Id != excludeId.Value) &&
            (x.Name == name.Trim() || (normalizedCode != null && x.Code == normalizedCode)), ct);

        if (exists)
            throw new InvalidOperationException("Ya existe un centro de costo con ese nombre o codigo.");
    }

    internal static async Task EnsureExpenseCategoryUnique(
        GrimorioDbContext db, Guid branchId, string name, Guid? excludeId, CancellationToken ct)
    {
        var exists = await db.ExpenseCategories.AnyAsync(x =>
            x.BranchId == branchId &&
            (!excludeId.HasValue || x.Id != excludeId.Value) &&
            x.Name == name.Trim(), ct);

        if (exists)
            throw new InvalidOperationException("Ya existe una categoria de gasto con ese nombre.");
    }
}

internal static class FinanceMapper
{
    internal static CostCenterDto MapCostCenter(CostCenter entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code,
        Description = entity.Description,
        DisplayOrder = entity.DisplayOrder,
        IsActive = entity.IsActive,
    };

    internal static ExpenseCategoryDto MapExpenseCategory(ExpenseCategory entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Type = entity.Type.ToString(),
        DisplayOrder = entity.DisplayOrder,
        IsActive = entity.IsActive,
    };

    internal static ExpenseDto MapExpense(Expense entity) => new()
    {
        Id = entity.Id,
        ExpenseDate = entity.ExpenseDate,
        CostCenterId = entity.CostCenterId,
        CostCenterName = entity.CostCenter?.Name ?? string.Empty,
        ExpenseCategoryId = entity.ExpenseCategoryId,
        ExpenseCategoryName = entity.ExpenseCategory?.Name ?? string.Empty,
        ExpenseCategoryType = entity.ExpenseCategory?.Type.ToString() ?? string.Empty,
        PaymentMethodConfigId = entity.PaymentMethodConfigId,
        PaymentMethodName = entity.PaymentMethodConfig?.Name,
        PaymentMethodColor = entity.PaymentMethodConfig?.Color,
        IsCashPayment = entity.PaymentMethodConfig?.IsCash == true,
        CashSessionId = entity.CashSessionId,
        CashRegisterName = entity.CashSession?.CashRegister?.Name,
        CashRegisterCode = entity.CashSession?.CashRegister?.Code,
        SupplierName = entity.SupplierName,
        DocumentNumber = entity.DocumentNumber,
        Amount = entity.Amount,
        Notes = entity.Notes,
        Status = entity.Status.ToString(),
        RegisteredAt = entity.RegisteredAt,
        RegisteredByName = entity.RegisteredByName,
        CancelledAt = entity.CancelledAt,
        CancelledByName = entity.CancelledByName,
        CancellationReason = entity.CancellationReason,
    };
}
