import 'dart:convert';

import 'order_models.dart';

/// Keeps the request unchanged until the server acknowledges it.
class PendingOrderItemsUpdate {
  final String _payload;

  PendingOrderItemsUpdate({
    required String idempotencyKey,
    required bool expectedIsDraft,
    required List<CartItem> items,
  }) : _payload = jsonEncode({
         'idempotencyKey': idempotencyKey,
         'expectedIsDraft': expectedIsDraft,
         'items': items
             .map(
               (item) => {
                 'menuItemId': item.menuItemId,
                 'quantity': item.quantity,
                 if (item.promotionId != null) 'promotionId': item.promotionId,
                 'notes': item.notes,
                 'isTakeout': item.isTakeout,
                 'modifierSelections': item.modifierSelections
                     .map(
                       (selection) => {
                         'modifierOptionId': selection.modifierOptionId,
                         'quantity': selection.quantity,
                       },
                     )
                     .toList(),
               },
             )
             .toList(),
       });

  Map<String, dynamic> toJson() => jsonDecode(_payload) as Map<String, dynamic>;
}
