namespace Grimorio.Application.DTOs;

public class OrderPrebillDto
{
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string? TableCode { get; set; }
    public string? WaiterName { get; set; }
    public DateTime PrintedAt { get; set; }
    public ThermalReceiptIssuerDto Issuer { get; set; } = new();
    public ThermalReceiptCustomerDto? Customer { get; set; }
    public List<OrderPrebillItemDto> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal SubtotalWithoutTax { get; set; }
    public decimal DiscountWithoutTax { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
}

public class OrderPrebillItemDto : ThermalReceiptItemDto
{
    public List<ModifierSelectionDto> ModifierSelections { get; set; } = [];
}
