import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:station_app/models/station_item.dart';
import 'package:station_app/providers/station_provider.dart';
import 'package:station_app/screens/kds_screen.dart';
import 'package:station_app/services/api_service.dart';
import 'package:station_app/services/auth_service.dart';
import 'package:station_app/services/kitchen_hub_service.dart';
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
  @override
  Future<void> connect(String url, String token, List<String> ids) async {
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

class FakeApi extends ApiService {
  FakeApi() : super(token: 'token');
  List<StationItem> active = [];
  List<StationItem> completed = [];
  Completer<List<StationItem>>? loading;
  Completer<void>? updating;
  int updates = 0;
  @override
  Future<List<StationItem>> getStationItems(String id) async =>
      loading == null ? active : await loading!.future;
  @override
  Future<List<StationItem>> getCompletedStationItems(String id) async =>
      completed;
  @override
  Future<void> updateItemStatus(String id, String status) async {
    updates++;
    await updating?.future;
  }
}

StationItem item(
  String id, {
  String status = 'Pending',
  String name = 'Salchipapa',
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
  orderNotes: 'Sin cubiertos',
  modifierSelections: [const ModifierSelection(optionName: 'BBQ', quantity: 2)],
);

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  late FakeHub hub;
  late FakeTts tts;
  late FakeApi api;
  late StationProvider provider;
  setUp(() async {
    hub = FakeHub();
    tts = FakeTts();
    api = FakeApi();
    provider = StationProvider(
      auth: FakeAuth(),
      hub: hub,
      tts: tts,
      apiFactory: (_) => api,
    );
    await provider.init();
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
      hub.onNewItems!([
        item('a', name: 'Combo 6 de alitas con salsa especial de la casa'),
      ]);
      await tester.pumpWidget(
        ChangeNotifierProvider.value(
          value: provider,
          child: const MaterialApp(home: KdsScreen()),
        ),
      );
      expect(tester.takeException(), isNull);
      expect(find.text('BBQ x2'), findsOneWidget);
      expect(find.text('Sin cubiertos'), findsOneWidget);
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
    expect(await update, isFalse);
    expect(
      provider.items.firstWhere((i) => i.orderItemId == 'a').status,
      'Ready',
    );
    expect(provider.items.any((i) => i.orderItemId == 'b'), isTrue);
    expect(provider.isUpdating('a'), isFalse);
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
