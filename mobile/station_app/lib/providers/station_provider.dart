import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../core/api_config.dart';
import '../models/completed_order.dart';
import '../models/display_size_mode.dart';
import '../models/station_item.dart';
import '../models/work_station.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/display_preferences_service.dart';
import '../services/kitchen_hub_service.dart';
import '../services/tts_service.dart';

enum AppState { unauthenticated, pickingStation, ready }

class StationProvider extends ChangeNotifier {
  final AuthService _auth;
  final KitchenHubService _hub;
  final TtsService _tts;
  final DisplayPreferencesService _displayPreferences;
  final ApiService Function(String token) _apiFactory;
  final List<Duration> _reconnectDelays;
  final Set<String> _updatingItems = {};
  List<VoidCallback>? _pendingEvents;
  int _session = 0;
  bool _disposed = false;
  bool _connecting = false;
  int _reconnectAttempt = 0;
  Timer? _reconnectTimer;

  StationProvider({
    AuthService? auth,
    KitchenHubService? hub,
    TtsService? tts,
    DisplayPreferencesService? displayPreferences,
    ApiService Function(String token)? apiFactory,
    List<Duration> reconnectDelays = const [
      Duration(seconds: 2),
      Duration(seconds: 5),
      Duration(seconds: 10),
      Duration(seconds: 30),
    ],
  }) : assert(reconnectDelays.isNotEmpty),
       _auth = auth ?? AuthService(),
       _hub = hub ?? KitchenHubService(),
       _tts = tts ?? TtsService(),
       _displayPreferences = displayPreferences ?? DisplayPreferencesService(),
       _apiFactory = apiFactory ?? ((token) => ApiService(token: token)),
       _reconnectDelays = reconnectDelays;

  bool isUpdating(String itemId) => _updatingItems.contains(itemId);

  void _handleEvent(VoidCallback event) {
    if (_disposed || appState != AppState.ready) return;
    if (_pendingEvents != null) {
      _pendingEvents!.add(event);
    } else {
      event();
    }
  }

  @override
  void notifyListeners() {
    if (!_disposed) super.notifyListeners();
  }

  AppState appState = AppState.unauthenticated;
  HubConnectionState connectionState = HubConnectionState.Disconnected;

  String _token = '';
  List<String> _stationIds = [];
  List<String> _stationNames = [];

  List<StationItem> items = [];
  List<CompletedOrder> completedOrders = [];

  String? errorMessage;
  bool isLoading = false;
  DisplaySizeMode displaySizeMode = DisplaySizeMode.automatic;

  String? get stationName {
    if (_stationNames.isEmpty) return null;
    if (_stationNames.length == 1) return _stationNames.first;
    return _stationNames.join(' + ');
  }

  String get serverUrl => ApiConfig.baseUrl;
  String get voiceSessionIdentity => _token;
  void setVoiceBusy(bool busy) => _tts.setVoiceBusy(busy);

  bool get ttsEnabled => _tts.enabled;
  void setTtsEnabled(bool value) {
    _tts.enabled = value;
    if (!value) _tts.stop();
    notifyListeners();
  }

  Future<void> setDisplaySizeMode(DisplaySizeMode mode) async {
    if (displaySizeMode == mode) return;
    displaySizeMode = mode;
    notifyListeners();
    await _displayPreferences.save(mode);
  }

  List<MapEntry<String, List<StationItem>>> get orderedGroups {
    final map = <String, List<StationItem>>{};
    for (final item in items) {
      map.putIfAbsent(item.orderId, () => []).add(item);
    }
    final entries = map.entries.toList()
      ..sort(
        (a, b) =>
            a.value.first.confirmedAt.compareTo(b.value.first.confirmedAt),
      );
    return entries;
  }

  Future<void> init() async {
    try {
      displaySizeMode = await _displayPreferences.load();
    } catch (e) {
      debugPrint('[Provider] No se pudo cargar el tamaño de pantalla: $e');
    }
    try {
      await _tts.init();
    } catch (e) {
      debugPrint('[Provider] Voz no disponible: $e');
      _tts.enabled = false;
    }

    _token = await _auth.getToken() ?? '';
    _stationIds = await _auth.getSavedStationIds();
    _stationNames = await _auth.getSavedStationNames();

    if (_token.isEmpty) {
      appState = AppState.unauthenticated;
    } else if (_stationIds.isEmpty) {
      appState = AppState.pickingStation;
    } else {
      appState = AppState.ready;
      await _startHub();
    }
    notifyListeners();
  }

  Future<List<KdsBranch>> loadKdsBranches() => _auth.getKdsBranches();

  Future<List<KdsUser>> loadKdsUsers(String branchId) =>
      _auth.getKdsUsers(branchId);

  Future<void> login(String branchId, String userId, String pin) async {
    isLoading = true;
    errorMessage = null;
    notifyListeners();

    try {
      _token = await _auth.login(branchId, userId, pin);
      _stationIds = await _auth.getSavedStationIds();
      _stationNames = await _auth.getSavedStationNames();

      if (_stationIds.isEmpty) {
        appState = AppState.pickingStation;
      } else {
        appState = AppState.ready;
        await _startHub();
      }
    } catch (e) {
      errorMessage = e.toString().replaceFirst('Exception: ', '');
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  Future<List<WorkStation>> loadStations() async {
    final api = _apiFactory(_token);
    return api.getStations();
  }

  Future<void> selectStations(List<WorkStation> stations) async {
    final selected = stations.where((s) => s.isActive).toList();
    if (selected.isEmpty) return;

    _stationIds = selected.map((s) => s.id).toList();
    _stationNames = selected.map((s) => s.name).toList();
    await _auth.saveStations(_stationIds, _stationNames);
    appState = AppState.ready;
    await _startHub();
    notifyListeners();
  }

  Future<void> _startHub({bool resetBackoff = true}) async {
    if (_connecting || _disposed || appState != AppState.ready) return;
    _reconnectTimer?.cancel();
    _reconnectTimer = null;
    if (resetBackoff) _reconnectAttempt = 0;
    _connecting = true;
    final session = ++_session;
    _hub.onResynchronized = _loadInitialItems;
    _hub.onOrderTableChanged = () => _handleEvent(() {
      unawaited(_loadInitialItems());
    });
    _hub.onConnectionChanged = (state) {
      if (session != _session || _disposed) return;
      connectionState = state;
      if (state == HubConnectionState.Connected) {
        _reconnectTimer?.cancel();
        _reconnectTimer = null;
        _reconnectAttempt = 0;
        errorMessage = null;
      } else if (state == HubConnectionState.Disconnected && !_connecting) {
        _scheduleReconnect(session);
        return;
      }
      notifyListeners();
    };

    _hub.onNewItems = (newItems) => _handleEvent(() {
      final brandNew = <String, List<StationItem>>{};
      final additions = <String, List<StationItem>>{};
      final knownOrders = {
        ...items.map((item) => item.orderId),
        ...completedOrders.map((order) => order.orderId),
      };
      final knownItemIds = {
        ...items.map((item) => item.orderItemId),
        ...completedOrders.expand(
          (order) => order.items.map((item) => item.orderItemId),
        ),
      };

      for (final item in newItems) {
        final existingIdx = items.indexWhere(
          (e) => e.orderItemId == item.orderItemId,
        );
        if (existingIdx != -1) {
          items[existingIdx] = item;
          continue;
        }

        if (knownItemIds.contains(item.orderItemId)) continue;
        if (item.status != 'Pending' && item.status != 'InPreparation') {
          continue;
        }
        knownItemIds.add(item.orderItemId);
        final previous = completedOrders.where(
          (e) => e.orderId == item.orderId,
        );
        if (previous.isNotEmpty) items.addAll(previous.first.items);
        completedOrders.removeWhere((e) => e.orderId == item.orderId);
        items.add(item);

        if (knownOrders.contains(item.orderId)) {
          additions.putIfAbsent(item.orderId, () => []).add(item);
        } else {
          brandNew.putIfAbsent(item.orderId, () => []).add(item);
        }
      }

      for (final entry in brandNew.entries) {
        _tts.enqueue(TtsService.buildAnnouncement(entry.value));
      }
      for (final entry in additions.entries) {
        _tts.enqueue(TtsService.buildAdditionAnnouncement(entry.value));
      }

      notifyListeners();
    });

    _hub.onItemUpdated = (orderItemId, orderId, status, notes) =>
        _handleEvent(() {
          if (status == 'Cancelled') {
            items.removeWhere((e) => e.orderItemId == orderItemId);
            completedOrders = completedOrders
                .map(
                  (order) => CompletedOrder(
                    orderId: order.orderId,
                    orderNumber: order.orderNumber,
                    orderLabel: order.orderLabel,
                    orderType: order.orderType,
                    orderNotes: order.orderNotes,
                    completedAt: order.completedAt,
                    items: List.unmodifiable(
                      order.items.where((e) => e.orderItemId != orderItemId),
                    ),
                  ),
                )
                .where((order) => order.items.isNotEmpty)
                .toList();
            _checkOrderCompletion(orderId);
            notifyListeners();
            return;
          }
          final idx = items.indexWhere((e) => e.orderItemId == orderItemId);
          if (idx != -1) {
            items[idx].status = status;
            if (notes != null && (items[idx].notes ?? '') != notes) {
              items[idx].notes = notes.isEmpty ? null : notes;
              _tts.enqueue(TtsService.buildItemNotesUpdated(items[idx], notes));
            }
            _checkOrderCompletion(orderId);
            notifyListeners();
          }
        });

    _hub.onOrderCancelled = (orderId) => _handleEvent(() {
      items.removeWhere((e) => e.orderId == orderId);
      completedOrders.removeWhere((e) => e.orderId == orderId);
      notifyListeners();
    });

    debugPrint(
      '[Provider] Conectando a ${ApiConfig.hubBaseUrl} con stationIds=$_stationIds',
    );
    try {
      await _hub.connect(ApiConfig.hubBaseUrl, _token, _stationIds);
      if (session != _session || _disposed) return;
      debugPrint('[Provider] Hub conectado OK');
      await _loadInitialItems();
    } catch (e) {
      if (session != _session || _disposed) return;
      if (e is UnauthorizedException) {
        debugPrint('[Provider] Token expirado, forzando re-login');
        await _forceLogout();
        return;
      }
      debugPrint('[Provider] Error en _startHub: $e');
      errorMessage = 'Sin conexión. Reintentando automáticamente...';
      _scheduleReconnect(session);
    } finally {
      if (session == _session) _connecting = false;
    }
  }

  void _scheduleReconnect(int session) {
    if (_disposed ||
        session != _session ||
        appState != AppState.ready ||
        _reconnectTimer?.isActive == true) {
      return;
    }

    connectionState = HubConnectionState.Reconnecting;
    errorMessage = 'Sin conexión. Reintentando automáticamente...';
    final index = _reconnectAttempt.clamp(0, _reconnectDelays.length - 1);
    final delay = _reconnectDelays[index];
    _reconnectAttempt++;
    debugPrint('[Provider] Próximo intento de conexión en ${delay.inSeconds}s');
    _reconnectTimer = Timer(delay, () {
      _reconnectTimer = null;
      if (_disposed || session != _session || appState != AppState.ready) {
        return;
      }
      unawaited(_startHub(resetBackoff: false));
    });
    notifyListeners();
  }

  void _cancelReconnect() {
    _reconnectTimer?.cancel();
    _reconnectTimer = null;
    _reconnectAttempt = 0;
    _connecting = false;
  }

  Future<void> _loadInitialItems() async {
    if (_pendingEvents != null || _disposed) return;
    final session = _session;
    final events = <VoidCallback>[];
    _pendingEvents = events;
    try {
      final api = _apiFactory(_token);
      final stationIds = List<String>.of(_stationIds);

      final activeItems = (await Future.wait(
        stationIds.map(api.getStationItems),
      )).expand((items) => items).toList();

      List<StationItem> completedItems = [];
      for (final stationId in stationIds) {
        try {
          completedItems.addAll(await api.getCompletedStationItems(stationId));
        } catch (e) {
          if (e is UnauthorizedException) rethrow;
          debugPrint(
            '[Provider] Completados no disponibles para $stationId: $e',
          );
        }
      }
      if (session != _session || _disposed) return;
      completedItems = _dedupeItems(completedItems);
      final activeOrders = activeItems.map((item) => item.orderId).toSet();
      items = _dedupeItems([
        ...activeItems,
        ...completedItems.where((item) => activeOrders.contains(item.orderId)),
      ]);

      final byOrder = <String, List<StationItem>>{};
      for (final item in completedItems) {
        if (activeOrders.contains(item.orderId)) continue;
        byOrder.putIfAbsent(item.orderId, () => []).add(item);
      }

      final sortedGroups = byOrder.entries.toList()
        ..sort((a, b) {
          final aTime = a.value
              .map((i) => i.updatedAt ?? i.confirmedAt)
              .reduce((max, current) => current.isAfter(max) ? current : max);
          final bTime = b.value
              .map((i) => i.updatedAt ?? i.confirmedAt)
              .reduce((max, current) => current.isAfter(max) ? current : max);
          return bTime.compareTo(aTime);
        });

      completedOrders = sortedGroups.map((entry) {
        final first = entry.value.first;
        final completedAt = entry.value
            .map((i) => i.updatedAt ?? i.confirmedAt)
            .reduce((max, current) => current.isAfter(max) ? current : max);
        return CompletedOrder(
          orderId: entry.key,
          orderNumber: first.orderNumber,
          orderLabel: first.orderLabel,
          orderType: first.orderType,
          orderNotes: first.orderNotes,
          completedAt: completedAt,
          items: List.unmodifiable(entry.value),
        );
      }).toList();

      errorMessage = null;
    } on UnauthorizedException {
      if (session == _session && !_disposed) await _forceLogout();
    } catch (e) {
      if (session == _session && !_disposed) {
        errorMessage = 'No se pudieron sincronizar los pedidos: $e';
      }
    } finally {
      if (identical(_pendingEvents, events)) _pendingEvents = null;
      if (session == _session && !_disposed) {
        for (final event in events) {
          event();
        }
        notifyListeners();
      }
    }
  }

  Future<void> _forceLogout() async {
    _cancelReconnect();
    _session++;
    _pendingEvents = null;
    await _tts.stop();
    await _hub.dispose();
    await _auth.logout();
    _token = '';
    _stationIds = [];
    _stationNames = [];
    items = [];
    completedOrders = [];
    errorMessage = 'Tu sesión expiró. Vuelve a iniciar sesión.';
    appState = AppState.unauthenticated;
    notifyListeners();
  }

  void _checkOrderCompletion(String orderId) {
    final orderItems = items.where((e) => e.orderId == orderId).toList();
    if (orderItems.isEmpty || !orderItems.every((e) => e.status == 'Ready')) {
      return;
    }

    final first = orderItems.first;
    if (!completedOrders.any((e) => e.orderId == orderId)) {
      completedOrders.insert(
        0,
        CompletedOrder(
          orderId: orderId,
          orderNumber: first.orderNumber,
          orderLabel: first.orderLabel,
          orderType: first.orderType,
          orderNotes: first.orderNotes,
          completedAt: DateTime.now(),
          items: List.unmodifiable(orderItems),
        ),
      );
    }
    items.removeWhere((e) => e.orderId == orderId);
  }

  List<StationItem> _dedupeItems(List<StationItem> source) {
    final byId = <String, StationItem>{};
    for (final item in source) {
      byId[item.orderItemId] = item;
    }
    return byId.values.toList()
      ..sort((a, b) => a.confirmedAt.compareTo(b.confirmedAt));
  }

  Future<void> advanceItemStatus(StationItem item) async {
    const next = {'Pending': 'InPreparation', 'InPreparation': 'Ready'};
    final nextStatus = next[item.status];
    if (nextStatus == null) return;
    await advanceItemStatusTo(item, nextStatus);
  }

  Future<bool> advanceItemStatusTo(
    StationItem item,
    String targetStatus,
  ) async {
    if (item.status == targetStatus) return true;
    if (!_updatingItems.add(item.orderItemId)) return false;
    final session = _session;
    final previousStatus = item.status;
    notifyListeners();

    final api = _apiFactory(_token);
    try {
      var attempt = 0;
      while (session == _session && !_disposed) {
        final current = items.where((e) => e.orderItemId == item.orderItemId);
        if (current.isEmpty) return false;
        if (_statusReached(current.first.status, targetStatus)) return true;
        if (current.first.status != previousStatus) return false;

        try {
          await api.updateItemStatus(item.orderItemId, targetStatus);
          if (session != _session || _disposed) return false;
          final latest = items.where((e) => e.orderItemId == item.orderItemId);
          if (latest.isNotEmpty && latest.first.status == previousStatus) {
            latest.first.status = targetStatus;
            _checkOrderCompletion(item.orderId);
          }
          return true;
        } on UnauthorizedException {
          rethrow;
        } catch (e) {
          if (session != _session || _disposed) return false;
          final index = attempt.clamp(0, _reconnectDelays.length - 1);
          final delay = _reconnectDelays[index];
          attempt++;
          errorMessage = 'Sincronizando cambios pendientes automáticamente...';
          debugPrint(
            '[Provider] Reintentando ${item.orderItemId} en '
            '${delay.inSeconds}s: $e',
          );
          notifyListeners();
          await Future<void>.delayed(delay);
        }
      }
      return false;
    } on UnauthorizedException {
      if (session == _session && !_disposed) await _forceLogout();
      return false;
    } finally {
      _updatingItems.remove(item.orderItemId);
      if (_updatingItems.isEmpty &&
          errorMessage?.startsWith('Sincronizando cambios pendientes') ==
              true) {
        errorMessage = null;
      }
      notifyListeners();
    }
  }

  bool _statusReached(String currentStatus, String targetStatus) {
    const rank = {'Pending': 0, 'InPreparation': 1, 'Ready': 2};
    final current = rank[currentStatus];
    final target = rank[targetStatus];
    return current != null && target != null && current >= target;
  }

  Future<void> reconnect() async {
    errorMessage = null;
    notifyListeners();
    await _startHub(resetBackoff: true);
  }

  void clearError() {
    errorMessage = null;
    notifyListeners();
  }

  Future<void> changeStation() async {
    _cancelReconnect();
    _session++;
    _pendingEvents = null;
    await _tts.stop();
    await _hub.dispose();
    await _auth.clearStation();
    _stationIds = [];
    _stationNames = [];
    items = [];
    completedOrders = [];
    appState = AppState.pickingStation;
    notifyListeners();
  }

  Future<void> logout() async {
    _cancelReconnect();
    _session++;
    _pendingEvents = null;
    await _tts.stop();
    await _hub.dispose();
    await _auth.logout();
    _token = '';
    _stationIds = [];
    _stationNames = [];
    items = [];
    completedOrders = [];
    appState = AppState.unauthenticated;
    notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    _cancelReconnect();
    _session++;
    _hub.dispose();
    _tts.dispose();
    super.dispose();
  }
}
