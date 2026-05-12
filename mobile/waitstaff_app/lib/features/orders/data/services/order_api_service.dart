import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_client.dart' show dioProvider;
import '../models/order_models.dart';

class OrderApiService {
  OrderApiService(this._ref);

  final Ref _ref;

  Future<List<TableDto>> getTables() async {
    final dio = _ref.read(dioProvider);
    final res = await dio.get('/TableService/tables');
    final list = res.data as List<dynamic>;
    return list
        .map((e) => TableDto.fromJson(e as Map<String, dynamic>))
        .where((m) => m.isActive)
        .toList();
  }

  Future<List<MenuCategoryDto>> getCategories() async {
    final dio = _ref.read(dioProvider);
    final res = await dio.get('/menu/categorias');
    final list = res.data as List<dynamic>;
    return list
        .map((e) => MenuCategoryDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<MenuItemDto>> getMenuItems({String? categoryId}) async {
    final dio = _ref.read(dioProvider);
    final res = await dio.get(
      '/menu/items',
      queryParameters: {
        'activeOnly': true,
        if (categoryId != null) 'categoryId': categoryId,
      },
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => MenuItemDto.fromJson(e as Map<String, dynamic>))
        .where((i) => i.isActive)
        .toList();
  }

  Future<List<OrderDto>> getOrders({bool activeOnly = true}) async {
    final dio = _ref.read(dioProvider);
    final res = await dio.get(
      '/pos/ordenes',
      queryParameters: {'activeOnly': activeOnly},
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => OrderDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<OrderDto> createOrder({
    required OrderType type,
    String? tableId,
    String? clientName,
    String? deliveryAddress,
    String? notes,
    required List<CartItem> items,
  }) async {
    final dio = _ref.read(dioProvider);
    final res = await dio.post('/pos/ordenes', data: {
      'type': type.apiValue,
      if (tableId != null) 'tableId': tableId,
      if (clientName != null && clientName.isNotEmpty) 'customerName': clientName,
      if (deliveryAddress != null && deliveryAddress.isNotEmpty)
        'deliveryAddress': deliveryAddress,
      if (notes != null && notes.isNotEmpty) 'notes': notes,
      'items': items
          .map((i) => {
                'menuItemId': i.menuItemId,
                'quantity': i.quantity,
                if (i.notes != null && i.notes!.isNotEmpty) 'notes': i.notes,
                if (i.ingredientChoices.isNotEmpty)
                  'ingredientChoices': i.ingredientChoices
                      .map((c) => {
                            'recipeIngredientId': c.recipeIngredientId,
                            'chosenArticleId': c.chosenArticleId,
                          })
                      .toList(),
              })
          .toList(),
    });
    return OrderDto.fromJson(res.data as Map<String, dynamic>);
  }

  Future<OrderDto> confirmOrder(String id) async {
    final dio = _ref.read(dioProvider);
    final res = await dio.post('/pos/ordenes/$id/confirmar');
    return OrderDto.fromJson(res.data as Map<String, dynamic>);
  }

  Future<OrderDto> cancelOrder(String id) async {
    final dio = _ref.read(dioProvider);
    final res = await dio.post('/pos/ordenes/$id/cancelar');
    return OrderDto.fromJson(res.data as Map<String, dynamic>);
  }
}

final orderApiServiceProvider = Provider<OrderApiService>(
  (ref) => OrderApiService(ref),
);
