using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Inventory;

public class InventoryCountArea : BaseEntity
{
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public ICollection<InventoryCountMember> Members { get; set; } = [];
    public ICollection<InventoryCountRule> Rules { get; set; } = [];
}

public class InventoryCountMember : BaseEntity
{
    public Guid AreaId { get; set; }
    public Guid UserId { get; set; }
}

public class InventoryCountRule : BaseEntity
{
    public Guid AreaId { get; set; }
    public Guid ArticleId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Frequency { get; set; } = "Daily";
    public int WeekDay { get; set; } = 1;
}

public class StaffInventoryCount : BaseEntity
{
    public Guid AreaId { get; set; }
    public string AreaName { get; set; } = "";
    public DateOnly BusinessDate { get; set; }
    public Guid CounterUserId { get; set; }
    public string CounterName { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public Guid? RecountOfId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public ICollection<StaffInventoryCountLine> Lines { get; set; } = [];
}

public class StaffInventoryCountLine : BaseEntity
{
    public Guid CountId { get; set; }
    public Guid ArticleId { get; set; }
    public Guid WarehouseId { get; set; }
    public string ArticleName { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public Guid UnitId { get; set; }
    public string UnitSymbol { get; set; } = "";
    public string Frequency { get; set; } = "Daily";
    public decimal? Quantity { get; set; }
    public decimal? ExpectedQuantity { get; set; }
    public DateTime? CountedAt { get; set; }
    public Guid? CountedBy { get; set; }
    public string? Notes { get; set; }
}
