import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:grimorio_attendance/main.dart';

void main() {
  setUp(() => FlutterSecureStorage.setMockInitialValues({}));

  testWidgets('muestra el estado inicial de vinculación', (tester) async {
    await tester.pumpWidget(const GrimorioAttendanceApp());
    await tester.pump();

    expect(find.text('Grimorio Asistencia'), findsOneWidget);
    expect(
      find.text(
        '1. Registra este identificador desde Personal → Asistencia → Kioscos.',
      ),
      findsOneWidget,
    );
  });

  testWidgets('vinculación horizontal usable con el teclado abierto', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(640, 360);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetViewInsets);
    await tester.pumpWidget(const GrimorioAttendanceApp());
    await tester.pumpAndSettle();
    final kioskId = find.byType(TextFormField).first;
    await tester.ensureVisible(kioskId);
    await tester.enterText(kioskId, 'kiosco-prueba');
    tester.view.viewInsets = const FakeViewPadding(bottom: 160);
    await tester.pumpAndSettle();
    final keyField = find.byType(TextFormField).last;
    await tester.ensureVisible(keyField);
    await tester.enterText(keyField, 'clave-prueba');
    await tester.ensureVisible(find.text('VALIDAR Y VINCULAR'));
    await tester.pumpAndSettle();
    expect(find.text('VALIDAR Y VINCULAR').hitTestable(), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
