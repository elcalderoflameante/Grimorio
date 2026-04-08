namespace Grimorio.Domain.Entities.POS;

public enum TableServiceRequestType
{
    Napkins = 1,
    Salt = 2,
    TomatoSauce = 3,
    Mayonnaise = 4,
    Chili = 5,
    Container = 6,
    Bill = 7,
    CallWaiter = 8,
    Custom = 99,
}

public enum TableServiceRequestStatus
{
    Pending = 1,
    Taken = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
}

public class RestaurantTable : Grimorio.SharedKernel.BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Area { get; set; }
    public int Capacity { get; set; } = 2;
    public string PublicToken { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsActive { get; set; } = true;

    public virtual ICollection<TableServiceRequest> ServiceRequests { get; set; } = new List<TableServiceRequest>();
}

public class TableServiceRequest : Grimorio.SharedKernel.BaseEntity
{
    public Guid RestaurantTableId { get; set; }
    public TableServiceRequestType Type { get; set; }
    public string? CustomMessage { get; set; }
    public TableServiceRequestStatus Status { get; set; } = TableServiceRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TakenAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? TakenByUserId { get; set; }
    public string? TakenByName { get; set; }
    public string? ClientFingerprint { get; set; }
    public string? SourceIp { get; set; }

    public virtual RestaurantTable? RestaurantTable { get; set; }
}
