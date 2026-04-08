using Grimorio.Domain.Entities.POS;

namespace Grimorio.Application.DTOs;

public class RestaurantTableDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Area { get; set; }
    public int Capacity { get; set; }
    public string PublicToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
}

public class PublicTableInfoDto
{
    public Guid TableId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Area { get; set; }
    public bool IsActive { get; set; }
}

public class TableServiceRequestDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
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
