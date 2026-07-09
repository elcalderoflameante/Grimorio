import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../core/api_config.dart';
import '../models/completed_order.dart';
import '../models/station_item.dart';
import '../models/work_station.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/kitchen_hub_service.dart';
import '../services/speech_command_service.dart';
import '../services/tts_service.dart';

enum AppState { unauthenticated, pickingStation, ready }

class StationProvider extends ChangeNotifier {
  final AuthService _auth = AuthService();
  final KitchenHubService _hub = KitchenHubService();
  final TtsService _tts = TtsService();
  final SpeechCommandService _speech = SpeechCommandService();

  AppState appState = AppState.unauthenticated;
  HubConnectionState connectionState = HubConnectionState.Disconnected;

  String _token = '';
  List<String> _stationIds = [];
  List<String> _stationNames = [];

  List<StationItem> items = [];
  List<CompletedOrder> completedOrders = [];

  String? errorMessage;
  bool isLoading = false;
  bool isListening = false;

  String? get stationName {
    if (_stationNames.isEmpty) return null;
    if (_stationNames.length == 1) return _stationNames.first;
    return _stationNames.join(' + ');
  }
  String get serverUrl => ApiConfig.baseUrl;

  bool get ttsEnabled => _tts.enabled;
  void setTtsEnabled(bool value) {
    _tts.enabled = value;
    if (!value) _tts.stop();
    notifyListeners();
  }

  bool get speechEnabled => _speech.enabled;
  Future<void> setSpeechEnabled(bool value) async {
    _speech.enabled = value;
    if (value && appState == AppState.ready) {
      await _speech.startListening();
    } else if (!value) {
      await _speech.stop();
    }
    notifyListeners();
  }

  Future<bool> openSpeechOfflineSettings() => _speech.openOfflineSettings();

  List<MapEntry<String, List<StationItem>>> get orderedGroups {
    final map = <String, List<StationItem>>{};
    for (final item in items) {
      map.putIfAbsent(item.orderId, () => []).add(item);
    }
    final entries = map.entries.toList()
      ..sort((a, b) => a.value.first.confirmedAt.compareTo(b.value.first.confirmedAt));
    return entries;
  }

  Future<void> init() async {
    await _tts.init();
    await _initSpeech();

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

  Future<void> _initSpeech() async {
    _tts.onSpeakingStarted = _speech.pauseForTts;
    _tts.onSpeakingFinished = _speech.resumeFromTts;

    _speech.onListeningChanged = (listening) {
      isListening = listening;
      notifyListeners();
    };

    _speech.onCommand = (cmd) async {
      final updatedItems = <StationItem>[];
      for (final item in cmd.items) {
        final updated = await advanceItemStatusTo(item, cmd.newStatus);
        if (updated) updatedItems.add(item);
      }
      if (updatedItems.isEmpty) return;

      final statusLabel = cmd.newStatus == 'InPreparation' ? 'en preparación' : 'listo';
      final names = updatedItems.map((i) => i.itemName).join(', ');
      _tts.enqueue('Oído chef. $names $statusLabel.');
    };

    _speech.onCommandNotUnderstood = (reason) => _tts.enqueue(reason);
    _speech.onUnavailable = (message) {
      errorMessage = message;
      notifyListeners();
    };

    await _speech.init();
  }

  Future<void> login(String email, String password) async {
    isLoading = true;
    errorMessage = null;
    notifyListeners();

    try {
      _token = await _auth.login(email, password);
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
    final api = ApiService(token: _token);
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

  Future<void> _startHub() async {
    _hub.onConnectionChanged = (state) {
      connectionState = state;
      notifyListeners();
    };

    _hub.onNewItems = (newItems) {
      final brandNew = <String, List<StationItem>>{};
      final additions = <String, List<StationItem>>{};

      for (final item in newItems) {
        final existingIdx = items.indexWhere((e) => e.orderItemId == item.orderItemId);
        if (existingIdx != -1) {
          items[existingIdx] = item;
          continue;
        }

        final orderOnScreen = items.any((e) => e.orderId == item.orderId);
        final orderCompleted = completedOrders.any((e) => e.orderId == item.orderId);
        items.add(item);

        if (orderCompleted) {
          completedOrders.removeWhere((e) => e.orderId == item.orderId);
          additions.putIfAbsent(item.orderId, () => []).add(item);
        } else if (orderOnScreen) {
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

      _speech.updateItems(items);
      notifyListeners();
    };

    _hub.onItemUpdated = (orderItemId, orderId, status, notes) {
      final idx = items.indexWhere((e) => e.orderItemId == orderItemId);
      if (idx != -1) {
        items[idx].status = status;
        if (notes != null) {
          items[idx].notes = notes.isEmpty ? null : notes;
          _tts.enqueue(TtsService.buildItemNotesUpdated(items[idx], notes));
        }
        _checkOrderCompletion(orderId);
        _speech.updateItems(items);
        notifyListeners();
      }
    };

    _hub.onOrderCancelled = (orderId) {
      items.removeWhere((e) => e.orderId == orderId);
      completedOrders.removeWhere((e) => e.orderId == orderId);
      _speech.updateItems(items);
      notifyListeners();
    };

    debugPrint('[Provider] Conectando a ${ApiConfig.hubBaseUrl} con stationIds=$_stationIds');
    try {
      await _hub.connect(ApiConfig.hubBaseUrl, _token, _stationIds);
      debugPrint('[Provider] Hub conectado OK');
      await _loadInitialItems();
      await _speech.startListening();
    } catch (e) {
      if (e is UnauthorizedException) {
        debugPrint('[Provider] Token expirado, forzando re-login');
        await _forceLogout();
        return;
      }
      debugPrint('[Provider] Error en _startHub: $e');
      errorMessage = 'Error de conexión: ${e.toString().replaceFirst('Exception: ', '')}';
      notifyListeners();
    }
  }

  Future<void> _loadInitialItems() async {
    final api = ApiService(token: _token);

    final activeItems = <StationItem>[];
    for (final stationId in _stationIds) {
      activeItems.addAll(await api.getStationItems(stationId));
    }
    items = _dedupeItems(activeItems);

    List<StationItem> completedItems = [];
    for (final stationId in _stationIds) {
      try {
        completedItems.addAll(await api.getCompletedStationItems(stationId));
      } catch (e) {
        if (e is UnauthorizedException) rethrow;
        debugPrint('[Provider] Completados no disponibles para $stationId: $e');
      }
    }
    completedItems = _dedupeItems(completedItems);

    final byOrder = <String, List<StationItem>>{};
    for (final item in completedItems) {
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

    _speech.updateItems(items);
    notifyListeners();
  }

  Future<void> _forceLogout() async {
    await _speech.stop();
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
    if (orderItems.isEmpty || !orderItems.every((e) => e.status == 'Ready')) return;

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

  Future<bool> advanceItemStatusTo(StationItem item, String targetStatus) async {
    if (item.status == targetStatus) return true;

    final previousStatus = item.status;
    final previousItems = List<StationItem>.from(items);
    final previousCompletedOrders = List<CompletedOrder>.from(completedOrders);

    item.status = targetStatus;
    _checkOrderCompletion(item.orderId);
    _speech.updateItems(items);
    notifyListeners();

    final api = ApiService(token: _token);
    try {
      await api.updateItemStatus(item.orderItemId, targetStatus);
      return true;
    } on UnauthorizedException {
      await _forceLogout();
      return false;
    } catch (e) {
      item.status = previousStatus;
      items = previousItems;
      completedOrders = previousCompletedOrders;
      _speech.updateItems(items);
      errorMessage = 'Sin sincronizar: ${e.toString().replaceFirst('Exception: ', '')}';
      notifyListeners();
      return false;
    }
  }

  Future<void> reconnect() async {
    errorMessage = null;
    notifyListeners();
    await _startHub();
  }

  void clearError() {
    errorMessage = null;
    notifyListeners();
  }

  Future<void> changeStation() async {
    await _speech.stop();
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
    await _speech.stop();
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
    _speech.dispose();
    _hub.dispose();
    _tts.dispose();
    super.dispose();
  }
}
