import 'order_models.dart';

double _roundDiscount(double value) {
  final scaled = value * 100;
  final lower = scaled.floor();
  return ((scaled - lower - 0.5).abs() < 1e-8
          ? (lower.isEven ? lower : lower + 1)
          : scaled.round()) /
      100;
}

Map<CartItem, double> cartPromotionDiscounts(
  List<CartItem> items,
  PromotionDto? Function(CartItem) promotionForItem,
) {
  final discounts = <CartItem, double>{};
  final groups = <String, List<int>>{};
  final promotions = <String, PromotionDto>{};
  for (var index = 0; index < items.length; index++) {
    final item = items[index];
    final promotion = promotionForItem(item);
    discounts[item] =
        promotion?.calculateDiscount(
          item.price,
          item.quantity,
          modifierUnitPriceDelta: item.modifierSelections.fold<double>(
            0,
            (sum, option) => sum + option.unitPriceDelta * option.quantity,
          ),
        ) ??
        0;
    if (promotion?.type != 'SecondUnitPercentage') continue;
    promotions[promotion!.id] = promotion;
    (groups[promotion.id] ??= []).add(index);
  }
  for (final entry in groups.entries) {
    final indexes = entry.value
      ..sort((a, b) {
        final price = items[b].price.compareTo(items[a].price);
        return price == 0 ? a.compareTo(b) : price;
      });
    var position = 0;
    var base = 0.0;
    var gross = 0.0;
    for (final index in indexes) {
      final item = items[index];
      base += ((position + item.quantity) ~/ 2 - position ~/ 2) * item.price;
      position += item.quantity;
      gross += item.subtotal;
    }
    final total = _roundDiscount(
      base * (promotions[entry.key]!.discountPercent ?? 0) / 100,
    );
    var cumulative = 0.0;
    var previous = 0.0;
    for (final index in indexes) {
      final item = items[index];
      cumulative += item.subtotal;
      final next = gross == 0
          ? 0.0
          : _roundDiscount(total * cumulative / gross);
      discounts[item] = _roundDiscount(next - previous);
      previous = next;
    }
  }
  return discounts;
}
