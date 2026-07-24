using Grimorio.Domain.Entities.POS;

namespace Grimorio.Application.DTOs;

public class RestaurantTableDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Area { get; set; }
    public int Capacity { get; set; }
    public string PublicToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public int PosX { get; set; }
    public int PosY { get; set; }
    public string CurrentStatus { get; set; } = "Free";
    public Guid? CurrentOrderId { get; set; }
    public DateTime? CurrentOrderStartedAt { get; set; }
    public decimal CurrentOrderTotal { get; set; }
    public decimal PendingPaymentTotal { get; set; }
}

public class PublicTableInfoDto
{
    public Guid TableId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Area { get; set; }
    public bool IsActive { get; set; }
}

public class TableServiceRequestDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string? TableArea { get; set; }
    public TableServiceRequestType Type { get; set; }
    public string? CustomMessage { get; set; }
    public TableServiceRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? TakenAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? TakenByUserId { get; set; }
    public string? TakenByName { get; set; }
}

public class PublicCreateTableServiceRequestDto
{
    public string TableToken { get; set; } = string.Empty;
    public TableServiceRequestType Type { get; set; }
    public string? CustomMessage { get; set; }
    public string? ClientFingerprint { get; set; }
}

public class PublicRequestStatusDto
{
    public Guid Id { get; set; }
    public TableServiceRequestStatus Status { get; set; }
}

public class PublicActiveTableRequestDto
{
    public Guid Id { get; set; }
    public TableServiceRequestStatus Status { get; set; }
}

public class PublicMenuCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int Order { get; set; }
}

public class PublicMenuItemDto
{
    public Guid Id { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public bool HasModifiers { get; set; }
    public List<PublicMenuItemModifierGroupDto> ModifierGroups { get; set; } = [];
}

public class PublicMenuItemModifierGroupDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowDuplicates { get; set; }
    public int DisplayOrder { get; set; }
    public List<PublicMenuItemModifierOptionDto> Options { get; set; } = [];
}

public class PublicMenuItemModifierOptionDto
{
    public Guid Id { get; set; }
    public Guid ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class PublicTableMenuDto
{
    public List<PublicMenuCategoryDto> Categories { get; set; } = [];
    public List<PublicMenuItemDto> Items { get; set; } = [];
}

public class PublicCreateDraftOrderDto
{
    public string TableToken { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = [];
}

public class PublicDraftOrderResultDto
{
    public OrderDto Order { get; set; } = new();
    public TableServiceRequestDto Notification { get; set; } = new();
}
