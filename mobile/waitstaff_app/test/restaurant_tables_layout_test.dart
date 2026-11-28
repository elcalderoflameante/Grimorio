import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';
import 'package:waitstaff_app/features/orders/presentation/widgets/restaurant_tables_grid.dart';

void main() {
  final now = DateTime.utc(2026, 11, 27, 20);
  final tables = [
    TableDto(
      id: 'occupied',
      code: '1',
      name: 'Mesa 1',
      isActive: true,
      currentStatus: 'Occupied',
      pendingPaymentTotal: 28.25,
      currentOrderStartedAt: now.subtract(const Duration(minutes: 45)),
    ),
    TableDto(
      id: 'large',
      code: 'Terraza familiar 123',
      name: 'Mesa larga',
      isActive: true,
      currentStatus: 'Draft',
      pendingPaymentTotal: 999999.99,
      currentOrderStartedAt: now.subtract(const Duration(hours: 123)),
    ),
    const TableDto(
      id: 'free',
      code: '3',
      name: 'Mesa 3',
      isActive: true,
      currentStatus: 'Free',
      capacity: 8,
    ),
  ];
  for (final width in [280.0, 320.0, 360.0, 600.0]) {
    for (final scale in [1.0, 1.3, 2.0, 3.0]) {
      testWidgets(
        'balances stay inside cards at width $width and text scale $scale',
        (tester) async {
          tester.view.devicePixelRatio = 1;
          tester.view.physicalSize = Size(width, 820);
          addTearDown(tester.view.resetPhysicalSize);
          addTearDown(tester.view.resetDevicePixelRatio);
          String? opened;
          await tester.pumpWidget(
            MaterialApp(
              builder: (context, child) => MediaQuery(
                data: MediaQuery.of(
                  context,
                ).copyWith(textScaler: TextScaler.linear(scale)),
                child: child!,
              ),
              home: Scaffold(
                body: SingleChildScrollView(
                  child: Padding(
                    padding: const EdgeInsets.all(14),
                    child: RestaurantTablesGrid(
                      tables: tables,
                      now: now,
                      onTap: (table) => opened = table.id,
                    ),
                  ),
                ),
              ),
            ),
          );
          await tester.pumpAndSettle();
          expect(tester.takeException(), isNull);
          for (final entry in {
            'occupied': '\$28.25',
            'large': '\$999999.99',
          }.entries) {
            final card = tester.getRect(find.byKey(ValueKey(entry.key)));
            final money = tester.getRect(find.text(entry.value));
            expect(card.inflate(0.1).contains(money.topLeft), isTrue);
            expect(card.inflate(0.1).contains(money.bottomRight), isTrue);
          }
          await tester.ensureVisible(find.text('Mesa 3'));
          await tester.tap(find.text('Mesa 3'));
          expect(opened, 'free');
          expect(tester.takeException(), isNull);
        },
      );
    }
  }
}
