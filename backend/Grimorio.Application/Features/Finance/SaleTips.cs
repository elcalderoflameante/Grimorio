using MediatR;

namespace Grimorio.Application.Features.Finance;

public record SaveSaleTipDto(decimal Amount, string Method);
public record SaleTipDto(Guid Id, Guid OrderPaymentId, int OrderNumber, DateTime SaleDate, DateTime RegisteredAt,
    decimal Amount, string Method, string RegisteredByName, string? UpdatedByName);
public record TipMethodTotalDto(string Method, decimal Amount, int Count);
public record SaleTipReportDto(decimal TotalAmount, int Count, List<TipMethodTotalDto> ByMethod, List<SaleTipDto> Items, int Page, int PageSize);
public record GetSaleTipQuery(Guid BranchId, Guid OrderPaymentId) : IRequest<SaleTipDto?>;
public record SaveSaleTipCommand(Guid BranchId, Guid OrderPaymentId, decimal Amount, string Method, string UserName) : IRequest<SaleTipDto>;
public record DeleteSaleTipCommand(Guid BranchId, Guid OrderPaymentId) : IRequest<bool>;
public record GetSaleTipReportQuery(Guid BranchId, DateTime? FromUtc, DateTime? ToUtc, string? Method, int Page = 1, int PageSize = 25) : IRequest<SaleTipReportDto>;
