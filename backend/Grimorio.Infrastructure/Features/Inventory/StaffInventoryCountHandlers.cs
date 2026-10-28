using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory;
using MediatR;

namespace Grimorio.Infrastructure.Features.Inventory;

public class StaffInventoryCountHandlers(StaffInventoryCountService service) :
    IRequestHandler<GetInventoryCountCatalogQuery, InventoryCountCatalogDto>,
    IRequestHandler<SaveInventoryCountAreaCommand, InventoryCountAreaDto>,
    IRequestHandler<GetMyInventoryCountsQuery, List<InventoryCountDueDto>>,
    IRequestHandler<StartInventoryCountCommand, StaffInventoryCountDto>,
    IRequestHandler<GetStaffInventoryCountQuery, StaffInventoryCountDto>,
    IRequestHandler<SaveInventoryCountLineCommand, StaffInventoryCountDto>,
    IRequestHandler<SubmitInventoryCountCommand, StaffInventoryCountDto>,
    IRequestHandler<ListStaffInventoryCountsQuery, List<StaffInventoryCountDto>>,
    IRequestHandler<ReviewInventoryCountCommand, StaffInventoryCountDto>
{
    public Task<InventoryCountCatalogDto> Handle(GetInventoryCountCatalogQuery r, CancellationToken ct) => service.Catalog(r.BranchId, ct);
    public Task<InventoryCountAreaDto> Handle(SaveInventoryCountAreaCommand r, CancellationToken ct) => service.SaveArea(r.BranchId, r.Value, ct);
    public Task<List<InventoryCountDueDto>> Handle(GetMyInventoryCountsQuery r, CancellationToken ct) => service.Mine(r.BranchId, ct);
    public Task<StaffInventoryCountDto> Handle(StartInventoryCountCommand r, CancellationToken ct) => service.Start(r.BranchId, r.AreaId, ct);
    public Task<StaffInventoryCountDto> Handle(GetStaffInventoryCountQuery r, CancellationToken ct) => service.Detail(r.BranchId, r.Id, r.Mine, ct);
    public Task<StaffInventoryCountDto> Handle(SaveInventoryCountLineCommand r, CancellationToken ct) => service.SaveLine(r.BranchId, r.Id, r.LineId, r.Value, ct);
    public Task<StaffInventoryCountDto> Handle(SubmitInventoryCountCommand r, CancellationToken ct) => service.Submit(r.BranchId, r.Id, ct);
    public Task<List<StaffInventoryCountDto>> Handle(ListStaffInventoryCountsQuery r, CancellationToken ct) => service.List(r.BranchId, r.From, r.To, r.Status, ct);
    public Task<StaffInventoryCountDto> Handle(ReviewInventoryCountCommand r, CancellationToken ct) => service.Review(r.BranchId, r.Id, r.Value, ct);
}
