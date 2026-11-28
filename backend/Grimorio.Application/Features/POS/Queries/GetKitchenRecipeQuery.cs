using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.POS.Queries;

public record GetKitchenRecipeQuery(Guid BranchId, Guid OrderItemId) : IRequest<KitchenRecipeDto?>;
