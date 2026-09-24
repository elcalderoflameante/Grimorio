import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../models/station_item.dart';

typedef ItemsCallback = void Function(List<StationItem> items);
typedef ItemUpdatedCallback =
    void Function(
      String orderItemId,
      String orderId,
      String status,
      String? notes,
    );
typedef OrderCancelledCallback = void Function(String orderId);
typedef ConnectionCallback = void Function(HubConnectionState state);

class KitchenHubService {
  HubConnection? _connection;

  ItemsCallback? onNewItems;
  ItemUpdatedCallback? onItemUpdated;
  OrderCancelledCallback? onOrderCancelled;
  ConnectionCallback? onConnectionChanged;
  Future<void> Function()? onResynchronized;

  Future<void> connect(
    String serverUrl,
    String token,
    List<String> stationIds,
  ) async {
    await dispose();

    final hubUrl = '$serverUrl/hubs/kitchen';

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

    final connection = _connection!;
    _connection!.onreconnecting(({Exception? error}) {
      if (identical(connection, _connection)) {
        onConnectionChanged?.call(HubConnectionState.Reconnecting);
      }
    });
    _connection!.onreconnected(({String? connectionId}) async {
      try {
        await _joinStations(connection, stationIds);
        if (!identical(connection, _connection)) return;
        await onResynchronized?.call();
        if (!identical(connection, _connection)) return;
        onConnectionChanged?.call(HubConnectionState.Connected);
      } catch (e) {
        debugPrint('[KitchenHub] Error al recuperar estaciones: $e');
        if (identical(connection, _connection)) {
          await dispose();
        }
      }
    });

    _connection!.onclose(({Exception? error}) {
      if (identical(connection, _connection)) {
        onConnectionChanged?.call(HubConnectionState.Disconnected);
      }
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
        data['notes'] as String?,
      );
    });

    _connection!.on('kitchen:order-cancelled', (args) {
      if (args == null || args.isEmpty) return;
      final data = args[0] as Map<String, dynamic>?;
      if (data == null) return;
      onOrderCancelled?.call(data['orderId'] as String);
    });

    try {
      await connection.start();
      if (!identical(connection, _connection)) return;
      await _joinStations(connection, stationIds);
    } catch (e) {
      debugPrint('[KitchenHub] Error al conectar: $e');
      rethrow;
    }
    if (identical(connection, _connection)) {
      onConnectionChanged?.call(HubConnectionState.Connected);
    }
  }

  Future<void> _joinStations(HubConnection connection, List<String> stationIds) async {
    for (final stationId in stationIds) {
      if (!identical(connection, _connection)) return;
      await connection.invoke('JoinStation', args: [stationId]);
    }
  }

  Future<void> dispose() async {
    final connection = _connection;
    _connection = null;
    onConnectionChanged?.call(HubConnectionState.Disconnected);
    if (connection != null) {
      await connection.stop();
    }
  }

  HubConnectionState get state =>
      _connection?.state ?? HubConnectionState.Disconnected;
}
