namespace Grimorio.Application.DTOs;

public class SeparateCashSessionDto
{
    public Guid RequestId { get; set; }
    public DateTimeOffset Cutoff { get; set; }
    public decimal NewOpeningBalance { get; set; }
    public decimal? PreviousActualCash { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PreviewToken { get; set; } = string.Empty;
}

public class CashSessionSeparationMovementDto
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public bool MovesToNewSession { get; set; }
    public bool IsCancelled { get; set; }
}

public class CashSessionSeparationPreviewDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime CutoffUtc { get; set; }
    public CashSessionDto PreviousSession { get; set; } = new();
    public CashSessionDto NewSession { get; set; } = new();
    public List<CashSessionSeparationMovementDto> Movements { get; set; } = [];
}

public class CashSessionSeparationResultDto
{
    public Guid Id { get; set; }
    public Guid PreviousSessionId { get; set; }
    public Guid NewSessionId { get; set; }
}

public class CashSessionSeparationRecordDto : CashSessionSeparationResultDto
{
    public DateTime CutoffUtc { get; set; }
    public DateTime RecordedAt { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public CashSessionSeparationPreviewDto Preview { get; set; } = new();
}
