import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/services/alert_service.dart';
import '../../data/models/table_service_models.dart';
import '../../data/services/table_service_api_service.dart';
import '../../data/services/table_service_signalr_service.dart';

enum TableRequestsConnectionStatus { connecting, connected, degraded }

class TableRequestsState {
  const TableRequestsState({
    this.requests = const [],
    this.isLoading = false,
    this.connectionStatus = TableRequestsConnectionStatus.connecting,
    this.errorMessage,
  });

  final List<TableServiceRequest> requests;
  final bool isLoading;
  final TableRequestsConnectionStatus connectionStatus;
  final String? errorMessage;

  TableRequestsState copyWith({
    List<TableServiceRequest>? requests,
    bool? isLoading,
    TableRequestsConnectionStatus? connectionStatus,
    String? errorMessage,
  }) {
    return TableRequestsState(
      requests: requests ?? this.requests,
      isLoading: isLoading ?? this.isLoading,
      connectionStatus: connectionStatus ?? this.connectionStatus,
      errorMessage: errorMessage,
    );
  }
}

class TableRequestsController extends StateNotifier<TableRequestsState> {
  TableRequestsController(this._ref) : super(const TableRequestsState());

  final Ref _ref;
  Timer? _refreshTimer;
  Timer? _reconnectTimer;
  bool _isInitialized = false;
  bool _isRefreshing = false;
  bool _isConnectingSignalR = false;

  Future<void> initialize() async {
    if (_isInitialized) return;

    _isInitialized = true;
    _startAutoRefresh();
    _connectSignalR();
    unawaited(_initializeAlerts());
    await _refreshRequests(showLoading: true);
  }

  Future<void> _initializeAlerts() async {
    try {
      await _ref.read(alertServiceProvider).initialize();
    } catch (e) {
      debugPrint('[Alerts] Initialization failed: $e');
    }
  }

  Future<void> loadRequests() async {
    await _refreshRequests(showLoading: true);
  }

  Future<void> _refreshRequests({required bool showLoading}) async {
    if (_isRefreshing) {
      return;
    }

    _isRefreshing = true;
    if (showLoading) {
      state = state.copyWith(isLoading: true, errorMessage: null);
    }

    try {
      final apiService = _ref.read(tableServiceApiServiceProvider);
      final requests = await apiService.getRequests();
      final sorted = List<TableServiceRequest>.from(requests)
        ..sort((a, b) => b.requestedAt.compareTo(a.requestedAt));
      state = state.copyWith(requests: sorted, isLoading: false);
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: 'Error al cargar solicitudes: $e',
      );
    } finally {
      _isRefreshing = false;
    }
  }

  Future<void> takeRequest(String id) async {
    try {
      final apiService = _ref.read(tableServiceApiServiceProvider);
      final updated = await apiService.takeRequest(id);
      _upsertRequest(updated);
    } catch (e) {
      state = state.copyWith(errorMessage: 'Error al tomar solicitud: $e');
    }
  }

  Future<void> startRequest(String id) async {
    try {
      final apiService = _ref.read(tableServiceApiServiceProvider);
      final updated = await apiService.setStatus(
        id,
        TableServiceRequestStatus.inProgress,
      );
      _upsertRequest(updated);
    } catch (e) {
      state = state.copyWith(errorMessage: 'Error al iniciar solicitud: $e');
    }
  }

  Future<void> completeRequest(String id) async {
    try {
      final apiService = _ref.read(tableServiceApiServiceProvider);
      final updated = await apiService.setStatus(
        id,
        TableServiceRequestStatus.completed,
      );
      _upsertRequest(updated);
    } catch (e) {
      state = state.copyWith(errorMessage: 'Error al completar solicitud: $e');
    }
  }

  void _connectSignalR() {
    final signalR = _ref.read(tableServiceSignalRServiceProvider);
    signalR.onConnecting = () {
      state = state.copyWith(
        connectionStatus:
            state.connectionStatus == TableRequestsConnectionStatus.connecting
            ? TableRequestsConnectionStatus.connecting
            : TableRequestsConnectionStatus.degraded,
      );
    };
    signalR.onConnected = () {
      _stopSignalRReconnect();
      state = state.copyWith(
        connectionStatus: TableRequestsConnectionStatus.connected,
      );
    };
    signalR.onDisconnected = () {
      state = state.copyWith(
        connectionStatus: TableRequestsConnectionStatus.degraded,
      );
      _startSignalRReconnect();
    };
    signalR.onAnyEvent = () {
      _refreshRequests(showLoading: false);
    };
    signalR.onNewRequest = (request) {
      _notifyIncomingRequest(request);
      _upsertRequest(request);
    };
    signalR.onRequestUpdated = (request) => _upsertRequest(request);
    _tryConnectSignalR();
  }

  Future<void> _tryConnectSignalR() async {
    if (_isConnectingSignalR) return;

    _isConnectingSignalR = true;
    try {
      await _ref.read(tableServiceSignalRServiceProvider).connect();
    } catch (e) {
      debugPrint('[SignalR] Connection failed: $e');
      state = state.copyWith(
        connectionStatus: TableRequestsConnectionStatus.degraded,
      );
      _startSignalRReconnect();
    } finally {
      _isConnectingSignalR = false;
    }
  }

  void _startSignalRReconnect() {
    if (_reconnectTimer != null) return;

    _reconnectTimer = Timer.periodic(const Duration(seconds: 12), (_) {
      _tryConnectSignalR();
    });
  }

  void _stopSignalRReconnect() {
    _reconnectTimer?.cancel();
    _reconnectTimer = null;
  }

  Future<void> _notifyIncomingRequest(TableServiceRequest request) async {
    try {
      await _ref.read(alertServiceProvider).notifyNewRequest(request);
    } catch (_) {
      // Alert failure should not block request updates.
    }
  }

  void _startAutoRefresh() {
    _refreshTimer?.cancel();
    _refreshTimer = Timer.periodic(const Duration(seconds: 8), (_) {
      _refreshRequests(showLoading: false);
    });
  }

  void _upsertRequest(TableServiceRequest incoming) {
    final list = List<TableServiceRequest>.from(state.requests);
    final idx = list.indexWhere((r) => r.id == incoming.id);
    if (idx >= 0) {
      list[idx] = incoming;
    } else {
      list.insert(0, incoming);
    }
    list.sort((a, b) => b.requestedAt.compareTo(a.requestedAt));
    state = state.copyWith(requests: list);
  }

  @override
  void dispose() {
    _refreshTimer?.cancel();
    _stopSignalRReconnect();
    _ref.read(tableServiceSignalRServiceProvider).disconnect();
    super.dispose();
  }
}

final tableRequestsControllerProvider =
    StateNotifierProvider<TableRequestsController, TableRequestsState>(
      (ref) => TableRequestsController(ref),
    );
