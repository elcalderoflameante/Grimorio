import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:grimorio_attendance/main.dart';

void main() {
  testWidgets('muestra el estado inicial de vinculación', (tester) async {
    await tester.pumpWidget(
      const ProviderScope(child: GrimorioAttendanceApp()),
    );
    await tester.pump();

    expect(find.text('Grimorio Asistencia'), findsOneWidget);
    expect(
      find.text(
        '1. Registra este identificador desde RR. HH. → Kioscos de asistencia.',
      ),
      findsOneWidget,
    );
  });
}
