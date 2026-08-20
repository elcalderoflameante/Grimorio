using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;

namespace Grimorio.Infrastructure.Features.POS;

internal static class PromotionEngine
{
    public static bool IsCurrentlyActive(Promotion promotion, DateTime localNow)
    {
        if (!promotion.IsActive || promotion.IsDeleted)
            return false;

        var localDate = DateOnly.FromDateTime(localNow);
        if (promotion.StartsOn.HasValue && localDate < promotion.StartsOn.Value)
            return false;
        if (promotion.EndsOn.HasValue && localDate > promotion.EndsOn.Value)
            return false;

        if (promotion.DaysOfWeekMask != 0)
        {
            var bit = 1 << (int)localNow.DayOfWeek;
            if ((promotion.DaysOfWeekMask & bit) == 0)
                return false;
        }

        if (promotion.StartsAt.HasValue || promotion.EndsAt.HasValue)
        {
            var localTime = TimeOnly.FromDateTime(localNow);
            var start = promotion.StartsAt ?? TimeOnly.MinValue;
            var end = promotion.EndsAt ?? TimeOnly.MaxValue;

            if (start <= end)
            {
                if (localTime < start || localTime > end)
                    return false;
            }
            else if (localTime < start && localTime > end)
            {
                return false;
            }
        }

        return true;
    }

    public static bool AppliesTo(Promotion promotion, MenuItem menuItem)
    {
        var appliesToItem = promotion.MenuItems.Any(x =>
            !x.IsDeleted &&
            x.MenuItemId == menuItem.Id);
        var appliesToCategory = promotion.MenuCategories.Any(x =>
            !x.IsDeleted &&
            x.MenuCategoryId == menuItem.MenuCategoryId);

        return appliesToItem || appliesToCategory;
    }

    public static PromotionCalculation Calculate(Promotion promotion, decimal unitPrice, int quantity, bool useCardPrice = false)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("La cantidad debe ser mayor a cero.");

        var gross = unitPrice * quantity;
        var discount = useCardPrice && promotion.PaymentPolicy == PromotionPaymentPolicy.CashTransferOnly
            ? 0m
            : useCardPrice && promotion.PaymentPolicy == PromotionPaymentPolicy.CardAlternativePrice && promotion.CardPrice.HasValue
                ? gross - (promotion.CardPrice.Value * quantity)
                : promotion.Type switch
        {
            PromotionType.Percentage => gross * ((promotion.DiscountPercent ?? 0m) / 100m),
            PromotionType.FixedAmount => promotion.DiscountAmount ?? 0m,
            PromotionType.FixedPrice => gross - ((promotion.FixedPrice ?? unitPrice) * quantity),
            PromotionType.BuyXPayY => CalculateBuyXPayY(promotion, unitPrice, quantity),
            _ => 0m,
        };

        discount = Math.Clamp(Math.Round(discount, 2), 0m, gross);
        var equivalentPct = gross > 0m
            ? Math.Round(discount / gross * 100m, 2)
            : 0m;

        return new PromotionCalculation(discount, equivalentPct);
    }

    public static PromotionDto Map(Promotion promotion, DateTime localNow) => new()
    {
        Id = promotion.Id,
        Name = promotion.Name,
        Description = promotion.Description,
        Type = promotion.Type.ToString(),
        IsActive = promotion.IsActive,
        StartsOn = promotion.StartsOn,
        EndsOn = promotion.EndsOn,
        StartsAt = promotion.StartsAt,
        EndsAt = promotion.EndsAt,
        DaysOfWeekMask = promotion.DaysOfWeekMask,
        DiscountPercent = promotion.DiscountPercent,
        DiscountAmount = promotion.DiscountAmount,
        FixedPrice = promotion.FixedPrice,
        PaymentPolicy = promotion.PaymentPolicy.ToString(),
        CardPrice = promotion.CardPrice,
        BuyQuantity = promotion.BuyQuantity,
        PayQuantity = promotion.PayQuantity,
        Priority = promotion.Priority,
        MenuItemIds = promotion.MenuItems
            .Where(x => !x.IsDeleted)
            .Select(x => x.MenuItemId)
            .ToList(),
        MenuCategoryIds = promotion.MenuCategories
            .Where(x => !x.IsDeleted)
            .Select(x => x.MenuCategoryId)
            .ToList(),
        IsCurrentlyActive = IsCurrentlyActive(promotion, localNow),
    };

    public static void Validate(Promotion promotion)
    {
        if (string.IsNullOrWhiteSpace(promotion.Name))
            throw new InvalidOperationException("El nombre de la promocion es obligatorio.");

        if (promotion.StartsOn.HasValue && promotion.EndsOn.HasValue && promotion.StartsOn > promotion.EndsOn)
            throw new InvalidOperationException("La fecha de inicio no puede ser mayor a la fecha fin.");

        if (promotion.DaysOfWeekMask < 0 || promotion.DaysOfWeekMask > 127)
            throw new InvalidOperationException("Los dias de la promocion no son validos.");

        if (promotion.PaymentPolicy == PromotionPaymentPolicy.CardAlternativePrice &&
            (!promotion.CardPrice.HasValue || promotion.CardPrice < 0))
        {
            throw new InvalidOperationException("El precio para tarjeta debe ser mayor o igual a cero.");
        }

        if (!promotion.MenuItems.Any(x => !x.IsDeleted) && !promotion.MenuCategories.Any(x => !x.IsDeleted))
            throw new InvalidOperationException("Debes seleccionar al menos un item o categoria para la promocion.");

        switch (promotion.Type)
        {
            case PromotionType.Percentage:
                if (!promotion.DiscountPercent.HasValue || promotion.DiscountPercent <= 0 || promotion.DiscountPercent > 100)
                    throw new InvalidOperationException("El porcentaje de descuento debe ser mayor a 0 y menor o igual a 100.");
                break;
            case PromotionType.FixedAmount:
                if (!promotion.DiscountAmount.HasValue || promotion.DiscountAmount <= 0)
                    throw new InvalidOperationException("El descuento fijo debe ser mayor a cero.");
                break;
            case PromotionType.FixedPrice:
                if (!promotion.FixedPrice.HasValue || promotion.FixedPrice < 0)
                    throw new InvalidOperationException("El precio promocional debe ser mayor o igual a cero.");
                break;
            case PromotionType.BuyXPayY:
                if (!promotion.BuyQuantity.HasValue || !promotion.PayQuantity.HasValue ||
                    promotion.BuyQuantity <= 1 ||
                    promotion.PayQuantity <= 0 ||
                    promotion.PayQuantity >= promotion.BuyQuantity)
                {
                    throw new InvalidOperationException("Para 3x2 u ofertas similares, la cantidad a pagar debe ser menor que la cantidad comprada.");
                }
                break;
        }
    }

    private static decimal CalculateBuyXPayY(Promotion promotion, decimal unitPrice, int quantity)
    {
        var buyQuantity = promotion.BuyQuantity ?? 0;
        var payQuantity = promotion.PayQuantity ?? 0;
        if (buyQuantity <= 1 || payQuantity <= 0 || payQuantity >= buyQuantity)
            return 0m;

        var groups = quantity / buyQuantity;
        var freeUnits = groups * (buyQuantity - payQuantity);
        return freeUnits * unitPrice;
    }
}

internal sealed record PromotionCalculation(decimal DiscountAmount, decimal DiscountPct);
