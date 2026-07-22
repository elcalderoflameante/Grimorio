import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../../../../core/constants/api_config.dart';
import '../../../../features/auth/data/services/auth_storage_service.dart';
import '../models/table_service_models.dart';

typedef RequestCallback = void Function(TableServiceRequest request);

class TableServiceSignalRService {
  TableServiceSignalRService(this._ref);

  final Ref _ref;
  HubConnection? _connection;

  VoidCallback? onConnecting;
  VoidCallback? onConnected;
  VoidCallback? onDisconnected;
  VoidCallback? onAnyEvent;
  RequestCallback? onNewRequest;
  RequestCallback? onRequestUpdated;

  Future<void> connect() async {
    if (_connection != null) return;

    onConnecting?.call();

    final storageService = _ref.read(authStorageServiceProvider);
    final token = await storageService.readAccessToken();

    final hubUrl = '${ApiConfig.hubBaseUrl}/hubs/table-service';

    final connection = HubConnectionBuilder()
        .withUrl(
          hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: token != null ? () async => token : null,
          ),
        )
        .withAutomaticReconnect()
        .build();

    connection.onclose(({Exception? error}) {
      debugPrint('[SignalR] Connection closed: $error');
      if (identical(_connection, connection)) {
        _connection = null;
      }
      onDisconnected?.call();
    });

    connection.onreconnecting(({Exception? error}) {
      debugPrint('[SignalR] Reconnecting: $error');
      onConnecting?.call();
    });

    connection.onreconnected(({String? connectionId}) {
      debugPrint('[SignalR] Reconnected: $connectionId');
      onConnected?.call();
    });

    connection.on(
      'tableService:new-request',
      (args) => _handleEvent('new-request', args, onNewRequest),
    );

    connection.on(
      'tableService:request-updated',
      (args) => _handleEvent('request-updated', args, onRequestUpdated),
    );

    _connection = connection;
    try {
      await (connection.start() ?? Future.value()).timeout(
        const Duration(seconds: 15),
        onTimeout: () => throw TimeoutException('SignalR connect timeout'),
      );
      onConnected?.call();
      debugPrint('[SignalR] Connected to $hubUrl');
    } catch (_) {
      if (identical(_connection, connection)) {
        _connection = null;
      }
      await connection.stop();
      rethrow;
    }
  }

  Future<void> disconnect() async {
    await _connection?.stop();
    _connection = null;
    onConnecting = null;
    onConnected = null;
    onDisconnected = null;
    onAnyEvent = null;
    onNewRequest = null;
    onRequestUpdated = null;
  }

  void _handleEvent(
    String eventName,
    List<Object?>? args,
    RequestCallback? callback,
  ) {
    onAnyEvent?.call();

    final payload = _extractPayload(args);
    if (payload == null) {
      debugPrint('[SignalR] $eventName payload could not be parsed: $args');
      return;
    }

    try {
      callback?.call(TableServiceRequest.fromJson(payload));
    } catch (e) {
      debugPrint('[SignalR] Error parsing $eventName: $e');
    }
  }

  Map<String, dynamic>? _extractPayload(List<Object?>? args) {
    if (args == null || args.isEmpty) {
      return null;
    }

    final first = args.first;
    if (first is Map) {
      return Map<String, dynamic>.from(first);
    }

    if (first is String) {
      final decoded = jsonDecode(first);
      if (decoded is Map) {
        return Map<String, dynamic>.from(decoded);
      }
    }

    return null;
  }
}

final tableServiceSignalRServiceProvider = Provider<TableServiceSignalRService>(
  (ref) => TableServiceSignalRService(ref),
);
