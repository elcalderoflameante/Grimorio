import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';
import 'package:waitstaff_app/features/orders/data/models/cart_promotion_pricing.dart';

void main() {
  const promotion = PromotionDto(
    id: 'burgers',
    name: 'Second burger',
    type: 'SecondUnitPercentage',
    discountPercent: 50,
  );
  CartItem burger(
    String id,
    double price, {
    int quantity = 1,
    String? promo = 'burgers',
  }) => CartItem(
    menuItemId: id,
    name: id,
    price: price,
    quantity: quantity,
    promotionId: promo,
  );
  int savings(List<CartItem> items) => cartPromotionDiscounts(
    items,
    (item) => item.promotionId == promotion.id ? promotion : null,
  ).values.fold(0, (sum, value) => sum + (value * 100).round());
  test('different burgers pair by promotion, not product or entry order', () {
    expect(savings([burger('classic', 4.25), burger('special', 5.25)]), 212);
    expect(savings([burger('special', 5.25), burger('classic', 4.25)]), 212);
    expect(promotion.valueLabel, '2da unidad: 50.0% desc.');
  });
  test('complete pairs use descending prices and preserve cents', () {
    expect(
      savings([burger('classic', 4.25), burger('special', 5.25, quantity: 2)]),
      262,
    );
    expect(
      savings([
        burger('classic', 4.25, quantity: 2),
        burger('special', 5.25, quantity: 2),
      ]),
      475,
    );
  });
  test('ordinary items and other promotions do not complete the pair', () {
    expect(
      savings([burger('classic', 4.25), burger('special', 5.25, promo: null)]),
      0,
    );
    expect(
      savings([
        burger('classic', 4.25),
        burger('special', 5.25, promo: 'other'),
      ]),
      0,
    );
  });
}
