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

    test('uses the order-specific balance and blocks a paid account', () {
      final order = OrderDto.fromJson({
        'id': 'order-1',
        'number': 12,
        'type': 'DineIn',
        'status': 'Confirmed',
        'subtotal': 24.50,
        'total': 24.50,
        'paidAmount': 24.50,
        'pendingPaymentTotal': 0,
        'paidAt': '2026-09-23T14:00:00Z',
        'items': <dynamic>[],
      });

      expect(order.paidAmount, 24.50);
      expect(order.pendingPaymentTotal, 0);
      expect(order.acceptsAdditionalItems, isFalse);
    });

    test('allows additions while an eligible order has a balance', () {
      final order = OrderDto.fromJson({
        'id': 'order-2',
        'number': 13,
        'type': 'DineIn',
        'status': 'InPreparation',
        'subtotal': 30,
        'total': 30,
        'paidAmount': 10,
        'pendingPaymentTotal': 20,
        'items': <dynamic>[],
      });

      expect(order.acceptsAdditionalItems, isTrue);
    });
  });
}
