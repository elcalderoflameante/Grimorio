import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';

void main() {
  const promotion = PromotionDto(
    id: 'offer',
    name: 'Lunch combo',
    type: 'FixedPrice',
    fixedPrice: 6.99,
  );
  test(
    'fixed-price promotion keeps optional meat on top of the base offer',
    () {
      expect(
        11.5 -
            promotion.calculateDiscount(11.5, 1, modifierUnitPriceDelta: 1.5),
        closeTo(8.49, 0.001),
      );
      expect(
        23 - promotion.calculateDiscount(11.5, 2, modifierUnitPriceDelta: 1.5),
        closeTo(16.98, 0.001),
      );
      expect(10 - promotion.calculateDiscount(10, 1), closeTo(6.99, 0.001));
    },
  );
}
