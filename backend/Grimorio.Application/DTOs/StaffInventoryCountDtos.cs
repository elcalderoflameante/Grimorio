namespace Grimorio.Application.DTOs;

public record CountOptionDto(Guid Id, string Name);
public record CountArticleOptionDto(Guid Id, string Name, string UnitSymbol);
public class InventoryCountAreaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<Guid> UserIds { get; set; } = [];
    public List<InventoryCountRuleDto> Rules { get; set; } = [];
}
public record InventoryCountRuleDto(Guid ArticleId, Guid WarehouseId, string Frequency, int WeekDay = 1);
public record InventoryCountCatalogDto(List<InventoryCountAreaDto> Areas, List<CountOptionDto> Users,
    List<CountArticleOptionDto> Articles, List<CountOptionDto> Warehouses);
public record InventoryCountDueDto(Guid AreaId, string AreaName, int PendingArticles, Guid? DraftId, string? CounterName, bool Recount);
public record InventoryCountLineDto(Guid Id, Guid ArticleId, string ArticleName, string WarehouseName,
    string UnitSymbol, decimal? Quantity, decimal? ExpectedQuantity, decimal? Difference, DateTime? CountedAt, string? Notes);
public record StaffInventoryCountDto(Guid Id, Guid AreaId, string AreaName, DateOnly BusinessDate,
    string CounterName, string Status, DateTime? SubmittedAt, DateTime? ReviewedAt, string? ReviewerName,
    string? ReviewNotes, Guid? RecountOfId, List<InventoryCountLineDto> Lines);
public record SaveInventoryCountLineDto(decimal? Quantity, string? Notes, DateTime? PreviousCountedAt = null);
public record ReviewInventoryCountDto(string Decision, string Notes);
