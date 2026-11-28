import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:provider/provider.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:station_app/models/kitchen_recipe.dart';
import 'package:station_app/providers/station_provider.dart';
import 'package:station_app/services/api_service.dart';
import 'package:station_app/widgets/order_card.dart';
import 'package:station_app/widgets/recipe_dialog.dart';
import 'station_provider_test.dart'
    show FakeApi, FakeAuth, FakeHub, FakeTts, FakeDisplayPreferences, item;

class RecipeApi extends FakeApi {
  int recipeCalls = 0;
  String? requestedItem;
  bool fail = false;
  KitchenRecipe recipe = const KitchenRecipe(
    name: 'Alitas crispy',
    ingredients: [
      RecipeIngredient(name: 'Alitas', quantity: 250, unit: 'g'),
      RecipeIngredient(
        name: 'Salsa base',
        quantity: 20,
        unit: 'g',
        ingredients: [
          RecipeIngredient(name: 'Mostaza', quantity: 10, unit: 'g'),
        ],
      ),
    ],
    preparation: RecipePreparation(
      estimatedMinutes: 12,
      steps: [
        RecipeStep(
          number: 1,
          instructions: 'Freir hasta el punto indicado.',
          isCritical: true,
        ),
      ],
    ),
  );

  @override
  Future<KitchenRecipe> getItemRecipe(String orderItemId) async {
    recipeCalls++;
    requestedItem = orderItemId;
    if (fail) throw Exception('Sin conexion');
    return recipe;
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  late RecipeApi api;
  late FakeHub hub;
  late StationProvider provider;

  setUp(() async {
    api = RecipeApi();
    hub = FakeHub();
    provider = StationProvider(
      auth: FakeAuth(),
      hub: hub,
      tts: FakeTts(),
      displayPreferences: FakeDisplayPreferences(),
      apiFactory: (_) => api,
    );
    await provider.init();
    hub.onNewItems!([
      item('component-task', name: 'Alitas (Combo: Combo 6)')
        ..notes = 'Sin picante',
    ]);
  });
  tearDown(() => provider.dispose());

  Future<void> openRecipe(WidgetTester tester, Size size) async {
    tester.view.physicalSize = size;
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      ChangeNotifierProvider.value(
        value: provider,
        child: MaterialApp(
          theme: ThemeData.dark(),
          home: Scaffold(
            body: Align(
              alignment: Alignment.topLeft,
              child: SizedBox(
                width: 240,
                child: OrderCard(orderId: 'order', items: provider.items),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.byKey(const ValueKey('recipe-component-task')));
    await tester.pumpAndSettle();
  }

  for (final size in [
    const Size(1280, 720),
    const Size(640, 360),
    const Size(360, 640),
  ]) {
    testWidgets('recipe is independent of status actions and fits $size', (
      tester,
    ) async {
      await openRecipe(tester, size);
      expect(find.byType(RecipeDialog), findsOneWidget);
      expect(api.requestedItem, 'component-task');
      expect(api.updates, 0);
      expect(provider.items.single.status, 'Pending');
      expect(find.text('Alitas crispy'), findsOneWidget);
      expect(find.text('250 g'), findsOneWidget);
      expect(find.text('Pedido: Sin cubiertos'), findsOneWidget);
      expect(find.text('Plato: Sin picante'), findsOneWidget);
      expect(find.text('BBQ x2'), findsWidgets);
      expect(tester.takeException(), isNull);
      final scroll = find.descendant(
        of: find.byType(RecipeDialog),
        matching: find.byType(SingleChildScrollView),
      );
      await tester.drag(scroll, const Offset(0, -600));
      await tester.pumpAndSettle();
      expect(find.text('1. Freir hasta el punto indicado.'), findsOneWidget);
      hub.onConnectionChanged!(HubConnectionState.Reconnecting);
      await tester.pump();
      expect(find.text('Alitas crispy'), findsOneWidget);
      expect(api.recipeCalls, 1);
      await tester.tap(find.byTooltip('Cerrar receta'));
      await tester.pumpAndSettle();
      expect(find.byType(RecipeDialog), findsNothing);
      expect(api.updates, 0);
    });
  }

  testWidgets('recipe request can be retried without changing the order', (
    tester,
  ) async {
    api.fail = true;
    await openRecipe(tester, const Size(1024, 600));
    expect(find.text('No se pudo consultar la receta.'), findsOneWidget);
    api.fail = false;
    await tester.tap(find.text('Reintentar'));
    await tester.pumpAndSettle();
    expect(find.text('Alitas crispy'), findsOneWidget);
    expect(api.recipeCalls, 2);
    expect(api.updates, 0);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('empty recipes and preparation show their own empty states', (
    tester,
  ) async {
    api.recipe = const KitchenRecipe(name: 'Sin receta');
    await openRecipe(tester, const Size(1024, 600));
    expect(
      find.text('No hay ingredientes registrados para este plato.'),
      findsOneWidget,
    );
    expect(
      find.text('No hay instrucciones de preparacion registradas.'),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  test(
    'API requests the component ID with kitchen authentication and parses quantities',
    () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/pos/ordenes/items/task-id/receta');
        expect(request.headers['Authorization'], 'Bearer kitchen-token');
        return http.Response(
          jsonEncode({
            'name': 'Alitas',
            'ingredients': [
              {'name': 'Sal', 'quantity': 0.125, 'unit': 'g'},
            ],
            'preparation': {
              'steps': [
                {'stepNumber': 2, 'instructions': 'Servir', 'isCritical': true},
              ],
            },
          }),
          200,
        );
      });
      addTearDown(client.close);
      final recipe = await ApiService(
        token: 'kitchen-token',
        client: client,
      ).getItemRecipe('task-id');
      expect(recipe.ingredients.single.quantityLabel, '0,125 g');
      expect(recipe.preparation!.steps.single.isCritical, isTrue);
    },
  );
}
