import 'station_item.dart';

class CompletedOrder {
  final String orderId;
  final int orderNumber;
  final String orderLabel;
  final String orderType;
  final String? orderNotes;
  final DateTime completedAt;
  final List<StationItem> items;

  int get itemCount => items.length;

  CompletedOrder({
    required this.orderId,
    required this.orderNumber,
    required this.orderLabel,
    required this.orderType,
    this.orderNotes,
    required this.completedAt,
    required this.items,
  });
}
