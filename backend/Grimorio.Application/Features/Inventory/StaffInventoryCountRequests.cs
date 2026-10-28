using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Inventory;

public record GetInventoryCountCatalogQuery(Guid BranchId) : IRequest<InventoryCountCatalogDto>;
public record SaveInventoryCountAreaCommand(Guid BranchId, InventoryCountAreaDto Value) : IRequest<InventoryCountAreaDto>;
public record GetMyInventoryCountsQuery(Guid BranchId) : IRequest<List<InventoryCountDueDto>>;
public record StartInventoryCountCommand(Guid BranchId, Guid AreaId) : IRequest<StaffInventoryCountDto>;
public record GetStaffInventoryCountQuery(Guid BranchId, Guid Id, bool Mine) : IRequest<StaffInventoryCountDto>;
public record SaveInventoryCountLineCommand(Guid BranchId, Guid Id, Guid LineId, SaveInventoryCountLineDto Value) : IRequest<StaffInventoryCountDto>;
public record SubmitInventoryCountCommand(Guid BranchId, Guid Id) : IRequest<StaffInventoryCountDto>;
public record ListStaffInventoryCountsQuery(Guid BranchId, DateOnly? From, DateOnly? To, string? Status) : IRequest<List<StaffInventoryCountDto>>;
public record ReviewInventoryCountCommand(Guid BranchId, Guid Id, ReviewInventoryCountDto Value) : IRequest<StaffInventoryCountDto>;
