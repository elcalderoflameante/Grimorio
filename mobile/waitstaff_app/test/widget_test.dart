import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/auth/data/models/auth_models.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';

void main() {
  group('Waitstaff authentication models', () {
    test('parses a waiter with an access PIN', () {
      final user = PinUser.fromJson({
        'id': 'user-1',
        'firstName': 'Ana',
        'lastName': 'Pérez',
        'hasKdsPin': true,
      });

      expect(user.displayName, 'Ana Pérez');
      expect(user.hasPin, isTrue);
    });
  });

  group('Restaurant table models', () {
    test('parses the current order independently of its waiter', () {
      final table = TableDto.fromJson({
        'id': 'table-1',
        'code': '4',
        'area': 'Terraza',
        'isActive': true,
        'currentStatus': 'Occupied',
        'currentOrderId': 'order-1',
        'pendingPaymentTotal': 24.50,
      });

      expect(table.name, 'Mesa 4');
      expect(table.currentOrderId, 'order-1');
      expect(table.isFree, isFalse);
      expect(table.pendingPaymentTotal, 24.50);
    });

    test('stores takeout at item level', () {
      final item = CartItem(
        menuItemId: 'item-1',
        name: 'Hamburguesa',
        price: 8.50,
        isTakeout: true,
      );

      expect(item.isTakeout, isTrue);
      expect(item.subtotal, 8.50);
    });
  });
}
