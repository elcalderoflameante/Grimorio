using Grimorio.Application.DTOs;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Domain.Entities.POS;
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
