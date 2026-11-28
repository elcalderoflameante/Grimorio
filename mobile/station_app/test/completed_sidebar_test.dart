import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:station_app/models/completed_order.dart';
import 'package:station_app/providers/station_provider.dart';
import 'package:station_app/widgets/completed_sidebar.dart';
import 'station_provider_test.dart'
    show FakeApi, FakeAuth, FakeHub, FakeTts, FakeDisplayPreferences, item;

void main() {
  late StationProvider provider;

  setUp(() async {
    provider = StationProvider(
      auth: FakeAuth(),
      hub: FakeHub(),
      tts: FakeTts(),
      displayPreferences: FakeDisplayPreferences(),
      apiFactory: (_) => FakeApi(),
    );
    await provider.init();
  });
  tearDown(() => provider.dispose());

  final cases = [
    ('DineIn', 'Mesa 6', 'Pedido #7 - Mesa 6'),
    ('Delivery', '#7', 'Pedido #7 - A domicilio'),
    ('Takeout', '#7', 'Pedido #7 - Para llevar'),
    (
      'Delivery',
      'Cliente con nombre largo',
      'Pedido #7 - A domicilio - Cliente con nombre largo',
    ),
    (
      'Takeout',
      'Cliente con nombre largo',
      'Pedido #7 - Para llevar - Cliente con nombre largo',
    ),
    ('', '#7', 'Pedido #7'),
  ];

  for (final width in [140.0, 220.0]) {
    for (final (type, label, title) in cases) {
      testWidgets('completed $type / $label at width $width', (tester) async {
        provider.completedOrders = [
          CompletedOrder(
            orderId: 'order',
            orderNumber: 7,
            orderType: type,
            orderLabel: label,
            orderNotes: 'Observacion general del pedido',
            completedAt: DateTime.now().subtract(const Duration(minutes: 90)),
            items: [item('dish', status: 'Ready')],
          ),
        ];
        await tester.pumpWidget(
          ChangeNotifierProvider.value(
            value: provider,
            child: MaterialApp(
              theme: ThemeData.dark(),
              home: Scaffold(body: CompletedSidebar(width: width)),
            ),
          ),
        );

        expect(find.text(title), findsOneWidget);
        expect(tester.takeException(), isNull);
        final button = find.byTooltip('Ver pedido');
        expect(tester.getSize(button), const Size(36, 36));
        expect(
          tester.widget<Icon>(find.byIcon(Icons.visibility_outlined)).size,
          22,
        );
        expect(find.text('Salchipapa'), findsNothing);
        await tester.tap(button);
        await tester.pumpAndSettle();
        expect(find.text('Salchipapa'), findsOneWidget);
        expect(find.text(title), findsOneWidget);
        expect(find.text('Observacion general del pedido'), findsOneWidget);
        expect(find.byTooltip('Ocultar pedido'), findsOneWidget);
        expect(provider.completedOrders.single.items.single.status, 'Ready');
        await tester.tap(find.byTooltip('Ocultar pedido'));
        await tester.pumpAndSettle();
        expect(find.text('Salchipapa'), findsNothing);
        expect(tester.takeException(), isNull);
        await tester.pumpWidget(const SizedBox.shrink());
      });
    }
  }
}
