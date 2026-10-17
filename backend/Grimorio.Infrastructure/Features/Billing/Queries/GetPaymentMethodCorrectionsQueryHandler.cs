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
        return corrections.Select(c => new PaymentMethodCorrectionDto {
            Id = c.Id, PaymentLineId = c.PaymentLineId, CorrectedAt = c.CreatedAt, ActorName = c.ActorName, Reason = c.Reason,
            Before = JsonSerializer.Deserialize<PaymentLineDto>(c.BeforeJson)!, After = JsonSerializer.Deserialize<PaymentLineDto>(c.AfterJson)!,
        }).ToList();
    }
}
