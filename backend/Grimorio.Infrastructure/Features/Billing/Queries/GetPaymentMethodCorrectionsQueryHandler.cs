using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing.Queries;

public class GetPaymentMethodCorrectionsQueryHandler(GrimorioDbContext db)
    : IRequestHandler<GetPaymentMethodCorrectionsQuery, List<PaymentMethodCorrectionDto>>
{
    public async Task<List<PaymentMethodCorrectionDto>> Handle(GetPaymentMethodCorrectionsQuery request, CancellationToken ct)
    {
        var corrections = await db.PaymentMethodCorrections.AsNoTracking()
            .Where(c => c.BranchId == request.BranchId && c.OrderPaymentId == request.PaymentId)
            .OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        return corrections.Select(c => {
            using var json = JsonDocument.Parse(c.AfterJson);
            var afterLines = json.RootElement.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<List<PaymentLineDto>>(c.AfterJson)!
                : [JsonSerializer.Deserialize<PaymentLineDto>(c.AfterJson)!];
            return new PaymentMethodCorrectionDto {
            Id = c.Id, PaymentLineId = c.PaymentLineId, CorrectedAt = c.CreatedAt, ActorName = c.ActorName, Reason = c.Reason,
            Before = JsonSerializer.Deserialize<PaymentLineDto>(c.BeforeJson)!, After = afterLines[0], AfterLines = afterLines,
        }; }).ToList();
    }
}
