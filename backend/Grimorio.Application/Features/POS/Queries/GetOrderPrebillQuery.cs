using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.POS.Queries;

public record GetOrderPrebillQuery(Guid BranchId, Guid OrderId) : IRequest<OrderPrebillDto?>;
