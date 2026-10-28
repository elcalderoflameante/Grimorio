import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:inventory_app/api.dart';
import 'package:inventory_app/main.dart';

Json draft() => {
  'id': 'count',
  'areaName': 'Bar',
  'businessDate': '2026-10-26',
  'status': 'Draft',
  'lines': <Json>[
    <String, dynamic>{
      'id': 'line',
      'articleName': 'Cerveza',
      'warehouseName': 'Bar',
      'unitSymbol': 'u',
      'quantity': null,
      'countedAt': null,
      'notes': null,
    },
  ],
};

void main() {
  testWidgets('Zero is a completed observation and enables submission', (
    tester,
  ) async {
    final api = InventoryApi(
      client: MockClient((request) async {
        expect(request.method, 'PUT');
        expect(jsonDecode(request.body)['quantity'], 0);
        final result = draft();
        (result['lines'] as List).first['quantity'] = 0;
        return http.Response(jsonEncode(result), 200);
      }),
    );
    await tester.pumpWidget(
      MaterialApp(
        home: CountPage(api: api, initial: draft()),
      ),
    );
    expect(
      tester
          .widget<FilledButton>(
            find.widgetWithText(FilledButton, 'Enviar conteo (0/1)'),
          )
          .onPressed,
      isNull,
    );
    await tester.tap(find.text('Cerveza'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField).at(1), '0');
    await tester.tap(find.text('Guardar cantidad'));
    await tester.pumpAndSettle();
    expect(find.byType(AlertDialog), findsNothing);
    expect(
      tester
          .widget<FilledButton>(
            find.widgetWithText(FilledButton, 'Enviar conteo (1/1)'),
          )
          .onPressed,
      isNotNull,
    );
    api.close();
  });

  testWidgets(
    'Failed network save preserves input and leaves article pending',
    (tester) async {
      final api = InventoryApi(
        client: MockClient(
          (_) async =>
              http.Response('{"message":"Sin conexión al servidor"}', 503),
        ),
      );
      await tester.pumpWidget(
        MaterialApp(
          home: CountPage(api: api, initial: draft()),
        ),
      );
      await tester.tap(find.text('Cerveza'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField).at(1), '3,5');
      await tester.tap(find.text('Guardar cantidad'));
      await tester.pumpAndSettle();
      expect(
        tester.widget<TextField>(find.byType(TextField).at(1)).controller!.text,
        '3,5',
      );
      expect(
        find.textContaining('Tu cantidad sigue aquí para reintentar.'),
        findsOneWidget,
      );
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();
      expect(
        tester
            .widget<FilledButton>(
              find.widgetWithText(FilledButton, 'Enviar conteo (0/1)'),
            )
            .onPressed,
        isNull,
      );
      api.close();
    },
  );
}
