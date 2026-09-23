using Grimorio.Domain.Entities.POS;

namespace Grimorio.Infrastructure.Features.POS;

public static class OrderUpdateState
{
    public static void ValidateExpectedDraft(OrderStatus status, bool? expectedIsDraft)
    {
        if (expectedIsDraft.HasValue && expectedIsDraft.Value != (status == OrderStatus.Draft))
            throw new InvalidOperationException(
                "El estado del pedido cambió. Vuelve a abrir la cuenta antes de guardar productos.");
    }
}
