import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:station_app/models/station_item.dart';
import 'package:station_app/models/display_size_mode.dart';
import 'package:station_app/providers/station_provider.dart';
import 'package:station_app/screens/kds_screen.dart';
import 'package:station_app/services/api_service.dart';
import 'package:station_app/services/auth_service.dart';
import 'package:station_app/services/kitchen_hub_service.dart';
import 'package:station_app/services/display_preferences_service.dart';
import 'package:station_app/services/tts_service.dart';

class FakeAuth extends AuthService {
  @override
  Future<String?> getToken() async => 'token';
  @override
  Future<List<String>> getSavedStationIds() async => ['kitchen'];
  @override
  Future<List<String>> getSavedStationNames() async => ['Cocina'];
}

class FakeHub extends KitchenHubService {
  int connectCount = 0;
  int failuresBeforeSuccess = 0;

  @override
  Future<void> connect(String url, String token, List<String> ids) async {
    connectCount++;
    if (connectCount <= failuresBeforeSuccess) {
      throw TimeoutException('Servidor temporalmente no disponible');
    }
    onConnectionChanged?.call(HubConnectionState.Connected);
  }

  @override
  Future<void> dispose() async {}
}

class FakeTts extends TtsService {
  final messages = <String>[];
  @override
  Future<void> init() async {}
  @override
  void enqueue(String text) => messages.add(text);
  @override
  Future<void> stop() async {}
}

class FakeDisplayPreferences extends DisplayPreferencesService {
  DisplaySizeMode mode = DisplaySizeMode.automatic;

  @override
  Future<DisplaySizeMode> load() async => mode;

  @override
  Future<void> save(DisplaySizeMode value) async => mode = value;
}

class FakeApi extends ApiService {
  FakeApi() : super(token: 'token');
  List<StationItem> active = [];
  List<StationItem> completed = [];
  Completer<List<StationItem>>? loading;
  Completer<void>? updating;
  int updates = 0;
  int updateFailuresBeforeSuccess = 0;
  @override
  Future<List<StationItem>> getStationItems(String id) async =>
      loading == null ? active : await loading!.future;
  @override
  Future<List<StationItem>> getCompletedStationItems(String id) async =>
      completed;
  @override
  Future<void> updateItemStatus(String id, String status) async {
    updates++;
    if (updates <= updateFailuresBeforeSuccess) {
      throw TimeoutException('Servidor temporalmente no disponible');
    }
    await updating?.future;
  }
}

StationItem item(
  String id, {
  String status = 'Pending',
  String name = 'Salchipapa',
  String changeKind = '',
}) => StationItem(
  orderItemId: id,
  orderId: 'order',
  orderNumber: 7,
  orderType: 'DineIn',
  tableCode: '6',
  itemName: name,
  quantity: 2,
  status: status,
  confirmedAt: DateTime.utc(2026, 9, 23),
  changeKind: changeKind,
  orderNotes: 'Sin cubiertos',
  modifierSelections: [const ModifierSelection(optionName: 'BBQ', quantity: 2)],
);

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  late FakeHub hub;
  late FakeTts tts;
  late FakeApi api;
  late FakeDisplayPreferences displayPreferences;
  late StationProvider provider;
  setUp(() async {
    hub = FakeHub();
    tts = FakeTts();
    api = FakeApi();
    displayPreferences = FakeDisplayPreferences();
    provider = StationProvider(
      auth: FakeAuth(),
      hub: hub,
      tts: tts,
      displayPreferences: displayPreferences,
      apiFactory: (_) => api,
      reconnectDelays: const [Duration(milliseconds: 10)],
    );
    await provider.init();
  });

  test('display size is saved per device', () async {
    await provider.setDisplaySizeMode(DisplaySizeMode.large);

    expect(provider.displaySizeMode, DisplaySizeMode.large);
    expect(displayPreferences.mode, DisplaySizeMode.large);
  });

  test('ready dishes move below active dishes without reordering them', () {
    provider.items = [
      item('ready-first', status: 'Ready'),
      item('pending-first'),
      item('preparing', status: 'InPreparation'),
      item('ready-second', status: 'Ready'),
      item('additional'),
    ];

    expect(
      provider.orderedGroups.single.value.map((dish) => dish.orderItemId),
      [
        'pending-first',
        'preparing',
        'additional',
        'ready-first',
        'ready-second',
      ],
    );
  });

  test(
    'table transfer reloads labels and announces the new table once',
    () async {
      hub.onNewItems!([item('moved')]);
      tts.messages.clear();
      final moved = StationItem(
        orderItemId: 'moved',
        orderId: 'order',
        orderNumber: 7,
        orderType: 'DineIn',
        tableCode: '9',
        itemName: 'Salchipapa',
        quantity: 1,
        status: 'Pending',
        confirmedAt: DateTime.now(),
      );
      api.active = [moved];
      hub.onOrderTableChanged!('order', '9');
      await Future<void>.delayed(const Duration(milliseconds: 30));
      expect(provider.items.single.orderLabel, 'Mesa 9');
      expect(tts.messages, hasLength(1));
      expect(tts.messages.single, contains('Mesa 6, ahora mesa 9'));
      tts.messages.clear();
      api.active = [];
      api.completed = [
        StationItem(
          orderItemId: 'moved',
          orderId: 'order',
          orderNumber: 7,
          orderType: 'DineIn',
          tableCode: '10',
          itemName: 'Salchipapa',
          quantity: 1,
          status: 'Ready',
          confirmedAt: DateTime.now(),
        ),
      ];
      hub.onOrderTableChanged!('order', '10');
      await Future<void>.delayed(const Duration(milliseconds: 30));
      expect(provider.completedOrders.single.orderLabel, 'Mesa 10');
      expect(tts.messages, hasLength(1));
      expect(tts.messages.single, contains('Mesa 9, ahora mesa 10'));
    },
  );

  test('reconnects and reloads orders without user interaction', () async {
    api.active = [item('restored')];
    hub.onConnectionChanged!(HubConnectionState.Disconnected);

    await Future<void>.delayed(const Duration(milliseconds: 30));

    expect(hub.connectCount, 2);
    expect(provider.connectionState, HubConnectionState.Connected);
    expect(provider.items.single.orderItemId, 'restored');
    expect(provider.errorMessage, isNull);
  });

  test('retries automatically when the initial connection fails', () async {
    final flakyHub = FakeHub()..failuresBeforeSuccess = 1;
    final localApi = FakeApi()..active = [item('initial-retry')];
    final localProvider = StationProvider(
      auth: FakeAuth(),
      hub: flakyHub,
      tts: FakeTts(),
      displayPreferences: FakeDisplayPreferences(),
      apiFactory: (_) => localApi,
      reconnectDelays: const [Duration(milliseconds: 10)],
    );

    try {
      await localProvider.init();
      expect(localProvider.connectionState, HubConnectionState.Reconnecting);

      await Future<void>.delayed(const Duration(milliseconds: 30));

      expect(flakyHub.connectCount, 2);
      expect(localProvider.connectionState, HubConnectionState.Connected);
      expect(localProvider.items.single.orderItemId, 'initial-retry');
    } finally {
      localProvider.dispose();
    }
  });
  tearDown(() => provider.dispose());

  for (final size in [const Size(1024, 600), const Size(1280, 800)]) {
    testWidgets('KDS fits tablet $size with long item and modifier names', (
      tester,
    ) async {
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      const name =
          'Alitas del duende - Porcion de alitas con salsa especial (Combo: Cena del castillo)';
      hub.onNewItems!([item('a', name: name)]);
      await tester.pumpWidget(
        ChangeNotifierProvider.value(
          value: provider,
          child: const MaterialApp(home: KdsScreen()),
        ),
      );
      expect(tester.takeException(), isNull);
      expect(find.text('BBQ x2'), findsOneWidget);
      expect(find.text('Sin cubiertos'), findsOneWidget);
      final itemName = tester.widget<Text>(find.text(name));
      expect(itemName.softWrap, isTrue);
      expect(itemName.overflow, isNot(TextOverflow.ellipsis));
      final orderTitle = tester.widget<Text>(find.text('Pedido #7 - Mesa 6'));
      expect(orderTitle.maxLines, 2);
      expect(orderTitle.overflow, isNot(TextOverflow.ellipsis));
      final scrollbars = tester.widgetList<RawScrollbar>(
        find.byType(RawScrollbar),
      );
      expect(scrollbars, isNotEmpty);
      expect(scrollbars.every((bar) => bar.padding == EdgeInsets.zero), isTrue);
      await tester.pump(const Duration(minutes: 1));
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox.shrink());
    });
  }

  test('announces all new dishes once and ignores duplicate deliveries', () {
    hub.onNewItems!([item('a'), item('b', name: 'Combo 6')]);
    expect(tts.messages, hasLength(1));
    expect(tts.messages.single, contains('Pedido nuevo'));
    expect(tts.messages.single, contains('Combo 6'));
    expect(tts.messages.single, contains('BBQ x2'));
    expect(tts.messages.single, contains('Sin cubiertos'));
    hub.onNewItems!([item('a'), item('b')]);
    expect(tts.messages, hasLength(1));
    hub.onNewItems!([item('c')]);
    expect(tts.messages.last, startsWith('Item adicional'));
  });

  test('corrected options are announced as a change, not an addition', () {
    hub.onNewItems!([
      item('corrected', name: 'Combo 6', changeKind: 'Correction'),
    ]);

    expect(tts.messages, hasLength(1));
    expect(tts.messages.single, startsWith('Opciones actualizadas'));
    expect(tts.messages.single, contains('Combo 6'));
    expect(tts.messages.single, contains('BBQ x2'));
  });

  test('cancelled dish does not prevent remaining ready dishes completing', () {
    hub.onNewItems!([item('a'), item('b')]);
    hub.onItemUpdated!('a', 'order', 'Ready', null);
    hub.onItemUpdated!('b', 'order', 'Cancelled', null);
    expect(provider.items, isEmpty);
    expect(provider.completedOrders.single.items.single.orderItemId, 'a');
    hub.onItemUpdated!('a', 'order', 'Cancelled', null);
    expect(provider.completedOrders, isEmpty);
  });

  test('reconnect keeps ready dishes with their pending order', () async {
    api.active = [item('b')];
    api.completed = [item('a', status: 'Ready')];
    await hub.onResynchronized!();
    expect(provider.items, hasLength(2));
    expect(provider.completedOrders, isEmpty);
    hub.onItemUpdated!('b', 'order', 'Ready', null);
    expect(provider.completedOrders.single.items, hasLength(2));
  });

  test(
    'events arriving during reload are applied after the snapshot',
    () async {
      api.loading = Completer<List<StationItem>>();
      final reload = hub.onResynchronized!();
      hub.onItemUpdated!('a', 'order', 'Cancelled', null);
      hub.onNewItems!([item('b')]);
      api.loading!.complete([item('a')]);
      await reload;
      expect(provider.items.single.orderItemId, 'b');
    },
  );

  test('failed update cannot undo Alexa or remove a new dish', () async {
    hub.onNewItems!([item('a')]);
    api.updating = Completer<void>();
    final update = provider.advanceItemStatusTo(
      provider.items.first,
      'InPreparation',
    );
    expect(provider.isUpdating('a'), isTrue);
    await provider.advanceItemStatusTo(provider.items.first, 'InPreparation');
    expect(api.updates, 1);
    hub.onItemUpdated!('a', 'order', 'Ready', null);
    hub.onNewItems!([item('b')]);
    api.updating!.completeError(Exception('Sin red'));
    expect(await update, isTrue);
    expect(
      provider.items.firstWhere((i) => i.orderItemId == 'a').status,
      'Ready',
    );
    expect(provider.items.any((i) => i.orderItemId == 'b'), isTrue);
    expect(provider.isUpdating('a'), isFalse);
  });

  test('failed status update retries automatically', () async {
    hub.onNewItems!([item('retry-item')]);
    api.updateFailuresBeforeSuccess = 1;

    final updated = await provider.advanceItemStatusTo(
      provider.items.first,
      'InPreparation',
    );

    expect(updated, isTrue);
    expect(api.updates, 2);
    expect(provider.items.single.status, 'InPreparation');
    expect(provider.isUpdating('retry-item'), isFalse);
    expect(provider.errorMessage, isNull);
  });

  test('notes are announced only when changed, including removal', () {
    hub.onNewItems!([item('a')]);
    tts.messages.clear();
    hub.onItemUpdated!('a', 'order', 'Pending', 'Sin sal');
    hub.onItemUpdated!('a', 'order', 'Pending', 'Sin sal');
    hub.onItemUpdated!('a', 'order', 'Pending', '');
    expect(tts.messages, hasLength(2));
    expect(tts.messages.last, contains('eliminada'));
  });
}
