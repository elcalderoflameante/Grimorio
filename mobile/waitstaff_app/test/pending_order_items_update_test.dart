import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';
import 'package:waitstaff_app/features/orders/data/models/pending_order_items_update.dart';

void main() {
  test('retry preserves the original items even if the cart changes', () {
    final item = CartItem(
      menuItemId: 'plate-1',
      name: 'Plato',
      price: 10,
      quantity: 2,
      notes: 'Sin sal',
      isTakeout: true,
      modifierSelections: [
        CartModifierSelection(
          modifierOptionId: 'option-1',
          groupName: 'Extra',
          optionName: 'Queso',
          quantity: 1,
          unitPriceDelta: 2,
          isTracked: false,
        ),
      ],
    );
    final cart = [item];
    final request = PendingOrderItemsUpdate(
      idempotencyKey: 'request-1',
      expectedIsDraft: true,
      items: cart,
    );
    final original = request.toJson();
    item.quantity = 5;
    item.notes = 'Cambio';
    item.isTakeout = false;
    item.modifierSelections.clear();
    cart.clear();
    expect(request.toJson(), original);
    expect(request.toJson()['expectedIsDraft'], isTrue);
    expect(request.toJson()['idempotencyKey'], 'request-1');
  });

  test('mutating a serialized attempt cannot alter the next retry', () {
    final request = PendingOrderItemsUpdate(
      idempotencyKey: 'request-2',
      expectedIsDraft: false,
      items: [CartItem(menuItemId: 'plate-1', name: 'Plato', price: 10)],
    );
    final attempt = request.toJson();
    (attempt['items'] as List).clear();
    attempt['idempotencyKey'] = 'changed';
    expect(request.toJson()['items'], hasLength(1));
    expect(request.toJson()['idempotencyKey'], 'request-2');
    expect(request.toJson()['expectedIsDraft'], isFalse);
  });
}
