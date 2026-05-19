import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../models/station_item.dart';

typedef ItemsCallback = void Function(List<StationItem> items);
typedef ItemUpdatedCallback = void Function(String orderItemId, String orderId, String status);
typedef OrderCancelledCallback = void Function(String orderId);
typedef ConnectionCallback = void Function(HubConnectionState state);

class KitchenHubService {
  HubConnection? _connection;

  ItemsCallback? onNewItems;
  ItemUpdatedCallback? onItemUpdated;
  OrderCancelledCallback? onOrderCancelled;
  ConnectionCallback? onConnectionChanged;

  Future<void> connect(String serverUrl, String token, String stationId) async {
    await dispose();

    final hubUrl = '$serverUrl/hubs/kitchen?access_token=$token';

    _connection = HubConnectionBuilder()
        .withUrl(
          hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
            skipNegotiation: false,
          ),
        )
        .withAutomaticReconnect(retryDelays: [2000, 5000, 10000, 30000])
        .build();

    _connection!.onreconnecting(({Exception? error}) {
      onConnectionChanged?.call(HubConnectionState.Reconnecting);
    });

    _connection!.onreconnected(({String? connectionId}) {
      onConnectionChanged?.call(HubConnectionState.Connected);
      _joinStation(stationId);
    });

    _connection!.onclose(({Exception? error}) {
      onConnectionChanged?.call(HubConnectionState.Disconnected);
    });

    // ── Eventos desde el servidor ─────────────────────────────────────────

    _connection!.on('kitchen:new-items', (args) {
      if (args == null || args.isEmpty) return;
      final rawList = args[0];
      if (rawList is! List) return;
      final items = rawList
          .whereType<Map<String, dynamic>>()
          .map(StationItem.fromJson)
          .toList();
      onNewItems?.call(items);
    });

    _connection!.on('kitchen:item-updated', (args) {
      if (args == null || args.isEmpty) return;
      final data = args[0] as Map<String, dynamic>?;
      if (data == null) return;
      onItemUpdated?.call(
        data['orderItemId'] as String,
        data['orderId'] as String,
        data['status'] as String,
      );
    });

    _connection!.on('kitchen:order-cancelled', (args) {
      if (args == null || args.isEmpty) return;
      final data = args[0] as Map<String, dynamic>?;
      if (data == null) return;
      onOrderCancelled?.call(data['orderId'] as String);
    });

    try {
      await _connection!.start();
    } catch (e) {
      debugPrint('[KitchenHub] Error al conectar: $e');
      rethrow;
    }
    onConnectionChanged?.call(HubConnectionState.Connected);
    await _joinStation(stationId);
  }

  Future<void> _joinStation(String stationId) async {
    if (_connection?.state == HubConnectionState.Connected) {
      await _connection!.invoke('JoinStation', args: [stationId]);
    }
  }

  Future<void> dispose() async {
    if (_connection != null) {
      await _connection!.stop();
      _connection = null;
    }
  }

  HubConnectionState get state =>
      _connection?.state ?? HubConnectionState.Disconnected;
}
