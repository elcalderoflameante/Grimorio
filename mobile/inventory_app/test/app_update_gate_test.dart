import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_app/api.dart';
import 'package:inventory_app/main.dart';
import 'package:inventory_app/core/services/app_update_service.dart';
import 'package:inventory_app/core/widgets/app_update_gate.dart';

class PendingUpdateService extends AppUpdateService {
  final response = Completer<AppUpdateInfo?>();
  @override
  Future<AppUpdateInfo?> check() => response.future;
}

void main() {
  testWidgets(
    'update waits for the real count route and preserves unconfirmed input',
    (tester) async {
      final service = PendingUpdateService();
      final api = InventoryApi();
      addTearDown(service.dispose);
      addTearDown(api.close);
      await tester.pumpWidget(
        MaterialApp(
          builder: (_, child) => AppUpdateGate(service: service, child: child!),
          home: Builder(
            builder: (context) => Scaffold(
              body: TextButton(
                onPressed: () => Navigator.of(context).push(
                  MaterialPageRoute<void>(
                    builder: (_) => CountPage(
                      api: api,
                      initial: {
                        'id': 'draft',
                        'areaName': 'Bar',
                        'businessDate': '2026-10-26',
                        'status': 'Draft',
                        'lines': <Json>[
                          {
                            'id': 'line',
                            'articleName': 'Cerveza',
                            'warehouseName': 'Bar',
                            'unitSymbol': 'u',
                            'quantity': null,
                            'notes': null,
                            'countedAt': null,
                          },
                        ],
                      },
                    ),
                  ),
                ),
                child: const Text('Abrir conteo'),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('Abrir conteo'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cerveza'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField).at(1), '3,5');
      service.response.complete(
        AppUpdateInfo(
          versionCode: 3,
          versionName: '1.0.2',
          required: false,
          apkUri: Uri.parse(
            'https://example.com/downloads/grimorio-inventario.apk',
          ),
          sha256Hash: 'a' * 64,
          message: 'Nueva versión',
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Actualizar ahora'), findsNothing);
      expect(
        tester.widget<TextField>(find.byType(TextField).at(1)).controller!.text,
        '3,5',
      );
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();
      expect(find.text('Actualizar ahora'), findsNothing);
      await tester.pageBack();
      await tester.pumpAndSettle();
      expect(find.text('Actualizar ahora'), findsOneWidget);
      await tester.tap(find.text('Mas tarde'));
      await tester.pumpAndSettle();
      expect(find.text('Actualizar ahora'), findsNothing);
      expect(find.text('Abrir conteo'), findsOneWidget);
    },
  );

  testWidgets(
    'unavailable version server leaves the app usable without login',
    (tester) async {
      final service = PendingUpdateService();
      addTearDown(service.dispose);
      await tester.pumpWidget(
        MaterialApp(
          home: AppUpdateGate(
            service: service,
            child: const Text('Ingreso con PIN'),
          ),
        ),
      );
      service.response.completeError(StateError('Sin conexión'));
      await tester.pumpAndSettle();
      expect(find.text('Ingreso con PIN'), findsOneWidget);
      expect(find.text('Actualizar ahora'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );
}
