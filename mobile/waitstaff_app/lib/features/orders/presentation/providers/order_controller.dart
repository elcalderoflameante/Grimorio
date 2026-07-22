import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/models/order_models.dart';
import '../../data/services/order_api_service.dart';

class OrdersState {
  const OrdersState({
    this.orders = const [],
    this.isLoading = false,
    this.errorMessage,
  });

  final List<OrderDto> orders;
  final bool isLoading;
  final String? errorMessage;

  // Órdenes que aún no han sido entregadas ni canceladas
  List<OrderDto> get active => orders
      .where(
        (o) =>
            o.status != OrderStatus.delivered &&
            o.status != OrderStatus.cancelled,
      )
      .toList();

  OrdersState copyWith({
    List<OrderDto>? orders,
    bool? isLoading,
    String? errorMessage,
  }) => OrdersState(
    orders: orders ?? this.orders,
    isLoading: isLoading ?? this.isLoading,
    errorMessage: errorMessage,
  );
}

class OrdersController extends StateNotifier<OrdersState> {
  OrdersController(this._ref) : super(const OrdersState());

  final Ref _ref;
  Timer? _timer;

  Future<void> initialize() async {
    await load(showLoading: true);
    _timer = Timer.periodic(const Duration(seconds: 30), (_) {
      load(showLoading: false);
    });
  }

  Future<void> load({bool showLoading = true}) async {
    if (showLoading) {
      state = state.copyWith(isLoading: true, errorMessage: null);
    }
    try {
      final service = _ref.read(orderApiServiceProvider);
      final list = await service.getOrders(activeOnly: true);
      list.sort((a, b) => b.number.compareTo(a.number));
      state = state.copyWith(orders: list, isLoading: false);
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: 'Error al cargar pedidos: $e',
      );
    }
  }

  Future<void> cancel(String id) async {
    try {
      final service = _ref.read(orderApiServiceProvider);
      final updated = await service.cancelOrder(id);
      final list = state.orders.map((o) => o.id == id ? updated : o).toList();
      state = state.copyWith(orders: list);
    } catch (e) {
      state = state.copyWith(errorMessage: 'Error al cancelar: $e');
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}

final ordersControllerProvider =
    StateNotifierProvider<OrdersController, OrdersState>(
      (ref) => OrdersController(ref),
    );
