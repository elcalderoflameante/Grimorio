using Grimorio.Application.DTOs;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Features.TableService.Queries;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.TableService.Commands;

public class CreateRestaurantTableCommandHandler : IRequestHandler<CreateRestaurantTableCommand, RestaurantTableDto>
{
    private readonly GrimorioDbContext _context;

    public CreateRestaurantTableCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<RestaurantTableDto> Handle(CreateRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        var area = NormalizeArea(request.Area);
        var exists = await _context.RestaurantTables.AnyAsync(
            x => x.BranchId == request.BranchId && x.Code == code && x.Area == area && !x.IsDeleted,
            cancellationToken);

        if (exists)
            throw new InvalidOperationException("Ya existe una mesa con ese numero en esa area.");

        var entity = new RestaurantTable
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            Code = code,
            Area = area,
            Capacity = request.Capacity,
            PublicToken = Guid.NewGuid().ToString("N"),
            IsActive = true,
        };

        _context.RestaurantTables.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return MapTable(entity);
    }

    private static RestaurantTableDto MapTable(RestaurantTable table) => new()
    {
        Id = table.Id,
        BranchId = table.BranchId,
        Code = table.Code,
        Area = table.Area,
        Capacity = table.Capacity,
        PublicToken = table.PublicToken,
        IsActive = table.IsActive,
        PublicUrl = $"/mesa/{table.PublicToken}",
    };

    internal static string? NormalizeArea(string? area)
    {
        return string.IsNullOrWhiteSpace(area) ? null : area.Trim();
    }

}

public class UpdateRestaurantTableCommandHandler : IRequestHandler<UpdateRestaurantTableCommand, RestaurantTableDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateRestaurantTableCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<RestaurantTableDto> Handle(UpdateRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        var table = await _context.RestaurantTables.FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Mesa no encontrada.");

        var code = request.Code.Trim();
        var area = CreateRestaurantTableCommandHandler.NormalizeArea(request.Area);
        var duplicate = await _context.RestaurantTables.AnyAsync(
            x => x.Id != request.Id && x.BranchId == table.BranchId && x.Code == code && x.Area == area && !x.IsDeleted,
            cancellationToken);

        if (duplicate)
            throw new InvalidOperationException("Ya existe otra mesa con ese numero en esa area.");

        table.Code = code;
        table.Area = area;
        table.Capacity = request.Capacity;
        table.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return new RestaurantTableDto
        {
            Id = table.Id,
            BranchId = table.BranchId,
            Code = table.Code,
            Area = table.Area,
            Capacity = table.Capacity,
            PublicToken = table.PublicToken,
            IsActive = table.IsActive,
            PublicUrl = $"/mesa/{table.PublicToken}",
        };
    }
}

public class RegenerateRestaurantTableTokenCommandHandler : IRequestHandler<RegenerateRestaurantTableTokenCommand, RestaurantTableDto>
{
    private readonly GrimorioDbContext _context;

    public RegenerateRestaurantTableTokenCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<RestaurantTableDto> Handle(RegenerateRestaurantTableTokenCommand request, CancellationToken cancellationToken)
    {
        var table = await _context.RestaurantTables.FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Mesa no encontrada.");

        table.PublicToken = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync(cancellationToken);

        return new RestaurantTableDto
        {
            Id = table.Id,
            BranchId = table.BranchId,
            Code = table.Code,
            Area = table.Area,
            Capacity = table.Capacity,
            PublicToken = table.PublicToken,
            IsActive = table.IsActive,
            PublicUrl = $"/mesa/{table.PublicToken}",
        };
    }
}

public class DeleteRestaurantTableCommandHandler : IRequestHandler<DeleteRestaurantTableCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteRestaurantTableCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        var table = await _context.RestaurantTables.FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Mesa no encontrada.");

        table.IsDeleted = true;
        table.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class PublicCreateTableServiceRequestCommandHandler : IRequestHandler<PublicCreateTableServiceRequestCommand, TableServiceRequestDto>
{
    private readonly GrimorioDbContext _context;

    public PublicCreateTableServiceRequestCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<TableServiceRequestDto> Handle(PublicCreateTableServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var table = await _context.RestaurantTables
            .FirstOrDefaultAsync(x => x.PublicToken == request.TableToken && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Mesa no válida.");

        if (!table.IsActive)
            throw new InvalidOperationException("La mesa no está habilitada para solicitudes.");

        var cooldownFrom = DateTime.UtcNow.AddSeconds(-20);
        var hasRecent = await _context.TableServiceRequests.AnyAsync(
            x => x.RestaurantTableId == table.Id && x.RequestedAt >= cooldownFrom && !x.IsDeleted,
            cancellationToken);

        if (hasRecent)
            throw new InvalidOperationException("Espera unos segundos antes de enviar otra solicitud.");

        var entity = new TableServiceRequest
        {
            Id = Guid.NewGuid(),
            BranchId = table.BranchId,
            RestaurantTableId = table.Id,
            Type = request.Type,
            CustomMessage = string.IsNullOrWhiteSpace(request.CustomMessage) ? null : request.CustomMessage.Trim(),
            Status = TableServiceRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            ClientFingerprint = string.IsNullOrWhiteSpace(request.ClientFingerprint) ? null : request.ClientFingerprint.Trim(),
            SourceIp = string.IsNullOrWhiteSpace(request.SourceIp) ? null : request.SourceIp,
        };

        _context.TableServiceRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new TableServiceRequestDto
        {
            Id = entity.Id,
            BranchId = entity.BranchId,
            RestaurantTableId = table.Id,
            TableCode = table.Code,
            TableArea = table.Area,
            Type = entity.Type,
            CustomMessage = entity.CustomMessage,
            Status = entity.Status,
            RequestedAt = entity.RequestedAt,
            TakenAt = entity.TakenAt,
            CompletedAt = entity.CompletedAt,
            TakenByUserId = entity.TakenByUserId,
            TakenByName = entity.TakenByName,
        };
    }
}

public class PublicCreateDraftOrderCommandHandler : IRequestHandler<PublicCreateDraftOrderCommand, PublicDraftOrderResultDto>
{
    private readonly GrimorioDbContext _context;

    public PublicCreateDraftOrderCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<PublicDraftOrderResultDto> Handle(PublicCreateDraftOrderCommand request, CancellationToken cancellationToken)
    {
        var table = await _context.RestaurantTables
            .FirstOrDefaultAsync(x => x.PublicToken == request.TableToken && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Mesa no válida.");

        if (!table.IsActive)
            throw new InvalidOperationException("La mesa no está habilitada para pedidos.");

        if (request.Items.Count == 0)
            throw new InvalidOperationException("Agrega al menos un producto para enviar el pedido.");

        if (request.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("La cantidad de cada producto debe ser mayor a cero.");

        var hasActiveOrder = await _context.Orders.AnyAsync(o =>
            o.BranchId == table.BranchId &&
            o.TableId == table.Id &&
            !o.IsDeleted &&
            o.PaidAt == null &&
            o.Status != OrderStatus.Cancelled &&
            o.Status != OrderStatus.Delivered,
            cancellationToken);

        if (hasActiveOrder)
            throw new InvalidOperationException("Esta mesa ya tiene un pedido abierto. Un mesero puede ayudarte a agregar o modificar productos.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var itemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _context.MenuItems
            .Where(m => itemIds.Contains(m.Id) && m.BranchId == table.BranchId && !m.IsDeleted)
            .Include(m => m.Category)
            .Include(m => m.Station)
            .Include(m => m.TaxRate)
            .Include(m => m.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Article)
            .Include(m => m.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Unit)
            .Include(m => m.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Article)
            .Include(m => m.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Unit)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var articleIds = menuItems
            .SelectMany(item => item.Recipe.Where(r => !r.IsDeleted).Select(r => r.ArticleId))
            .Concat(menuItems.SelectMany(item => item.ModifierGroups
                .SelectMany(group => group.Options)
                .Where(option => option.ArticleId.HasValue)
                .Select(option => option.ArticleId!.Value)))
            .Distinct()
            .ToList();
        var (stockByArticle, conversions) = await PublicMenuAvailability.LoadStockInputsAsync(
            _context,
            table.BranchId,
            articleIds,
            cancellationToken);

        var number = await _context.Orders
            .Where(o => o.BranchId == table.BranchId)
            .MaxAsync(o => (int?)o.Number, cancellationToken) ?? 0;
        number++;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BranchId = table.BranchId,
            Number = number,
            Type = OrderType.DineIn,
            Status = OrderStatus.Draft,
            TableId = table.Id,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        decimal subtotal = 0, discountTotal = 0;
        decimal base15 = 0, base0 = 0, baseExempt = 0, iva15 = 0, ice = 0;
        foreach (var itemDto in request.Items)
        {
            var menuItem = menuItems.FirstOrDefault(m => m.Id == itemDto.MenuItemId)
                ?? throw new InvalidOperationException("Uno de los productos ya no está disponible.");

            if (menuItem.Category is null || !menuItem.Category.IsActive || menuItem.Category.IsDeleted)
                throw new InvalidOperationException($"{menuItem.Name} ya no está disponible.");

            if (!PublicMenuAvailability.IsMenuItemAvailable(menuItem, stockByArticle, conversions, itemDto.Quantity))
                throw new InvalidOperationException($"{menuItem.Name} ya no está disponible en este momento.");

            var modifierSelections = PosMapper.BuildModifierSelections(table.BranchId, itemDto, menuItem);
            foreach (var selection in modifierSelections)
            {
                var option = menuItem.ModifierGroups
                    .SelectMany(group => group.Options)
                    .FirstOrDefault(o => o.Id == selection.ModifierOptionId);

                if (option is null || !PublicMenuAvailability.IsModifierOptionAvailable(
                    option,
                    stockByArticle,
                    conversions,
                    selection.Quantity * itemDto.Quantity))
                {
                    throw new InvalidOperationException($"La opción {selection.OptionName} ya no está disponible.");
                }
            }

            var unitPrice = menuItem.Price + modifierSelections.Sum(s => s.UnitPriceDelta * s.Quantity);
            var (discountAmt, taxableBase, taxAmt, totalPrice) = PosMapper.CalcItem(unitPrice, itemDto.Quantity, itemDto.DiscountPct, menuItem.TaxRate?.Percentage);
            subtotal += unitPrice * itemDto.Quantity;
            discountTotal += discountAmt;
            PosMapper.ClassifyTax(menuItem.TaxRate?.SriCode, taxableBase, taxAmt, ref base15, ref base0, ref baseExempt, ref iva15, ref ice);

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                BranchId = table.BranchId,
                MenuItemId = itemDto.MenuItemId,
                StationId = menuItem.StationId,
                Quantity = itemDto.Quantity,
                UnitPrice = unitPrice,
                DiscountPct = itemDto.DiscountPct,
                DiscountAmount = discountAmt,
                TaxRateId = menuItem.TaxRateId,
                TaxAmount = taxAmt,
                TotalPrice = totalPrice,
                Notes = itemDto.Notes?.Trim(),
                IsTakeout = false,
                Status = OrderItemStatus.Pending,
            };

            foreach (var selection in modifierSelections)
                orderItem.ModifierSelections.Add(selection);

            order.Items.Add(orderItem);
        }

        order.Subtotal = subtotal;
        order.DiscountTotal = discountTotal;
        order.TaxableBase15 = base15;
        order.TaxableBase0 = base0;
        order.TaxableBaseExempt = baseExempt;
        order.Iva15 = iva15;
        order.Ice = ice;
        order.TaxAmount = iva15 + ice;
        order.Total = subtotal - discountTotal;

        var notification = new TableServiceRequest
        {
            Id = Guid.NewGuid(),
            BranchId = table.BranchId,
            RestaurantTableId = table.Id,
            Type = TableServiceRequestType.Custom,
            CustomMessage = $"Pedido #{number} pendiente de confirmar",
            Status = TableServiceRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            ClientFingerprint = string.IsNullOrWhiteSpace(request.ClientFingerprint) ? null : request.ClientFingerprint.Trim(),
            SourceIp = string.IsNullOrWhiteSpace(request.SourceIp) ? null : request.SourceIp,
        };

        _context.Orders.Add(order);
        _context.TableServiceRequests.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdOrder = await _context.Orders
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .FirstAsync(o => o.Id == order.Id, cancellationToken);

        return new PublicDraftOrderResultDto
        {
            Order = PosMapper.MapOrder(createdOrder),
            Notification = new TableServiceRequestDto
            {
                Id = notification.Id,
                BranchId = notification.BranchId,
                RestaurantTableId = table.Id,
                TableCode = table.Code,
                TableArea = table.Area,
                Type = notification.Type,
                CustomMessage = notification.CustomMessage,
                Status = notification.Status,
                RequestedAt = notification.RequestedAt,
                TakenAt = notification.TakenAt,
                CompletedAt = notification.CompletedAt,
                TakenByUserId = notification.TakenByUserId,
                TakenByName = notification.TakenByName,
            },
        };
    }
}

public class TakeTableServiceRequestCommandHandler : IRequestHandler<TakeTableServiceRequestCommand, TableServiceRequestDto>
{
    private readonly GrimorioDbContext _context;

    public TakeTableServiceRequestCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<TableServiceRequestDto> Handle(TakeTableServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TableServiceRequests
            .Include(x => x.RestaurantTable)
            .FirstOrDefaultAsync(x => x.Id == request.RequestId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Solicitud no encontrada.");

        if (entity.Status != TableServiceRequestStatus.Pending)
            throw new InvalidOperationException("La solicitud ya no está pendiente.");

        entity.Status = TableServiceRequestStatus.Taken;
        entity.TakenByUserId = request.UserId;
        entity.TakenByName = request.UserName;
        entity.TakenAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var table = entity.RestaurantTable!;
        return new TableServiceRequestDto
        {
            Id = entity.Id,
            BranchId = entity.BranchId,
            RestaurantTableId = entity.RestaurantTableId,
            TableCode = table.Code,
            TableArea = table.Area,
            Type = entity.Type,
            CustomMessage = entity.CustomMessage,
            Status = entity.Status,
            RequestedAt = entity.RequestedAt,
            TakenAt = entity.TakenAt,
            CompletedAt = entity.CompletedAt,
            TakenByUserId = entity.TakenByUserId,
            TakenByName = entity.TakenByName,
        };
    }
}

public class SetTableServiceRequestStatusCommandHandler : IRequestHandler<SetTableServiceRequestStatusCommand, TableServiceRequestDto>
{
    private readonly GrimorioDbContext _context;

    public SetTableServiceRequestStatusCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<TableServiceRequestDto> Handle(SetTableServiceRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TableServiceRequests
            .Include(x => x.RestaurantTable)
            .FirstOrDefaultAsync(x => x.Id == request.RequestId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Solicitud no encontrada.");

        entity.Status = request.Status;
        if (request.Status == TableServiceRequestStatus.Completed || request.Status == TableServiceRequestStatus.Cancelled)
        {
            entity.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var table = entity.RestaurantTable!;
        return new TableServiceRequestDto
        {
            Id = entity.Id,
            BranchId = entity.BranchId,
            RestaurantTableId = entity.RestaurantTableId,
            TableCode = table.Code,
            TableArea = table.Area,
            Type = entity.Type,
            CustomMessage = entity.CustomMessage,
            Status = entity.Status,
            RequestedAt = entity.RequestedAt,
            TakenAt = entity.TakenAt,
            CompletedAt = entity.CompletedAt,
            TakenByUserId = entity.TakenByUserId,
            TakenByName = entity.TakenByName,
        };
    }
}
