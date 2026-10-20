import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/core/services/app_update_service.dart';
import 'package:waitstaff_app/core/widgets/app_update_gate.dart';

class PendingUpdateService extends AppUpdateService {
  final response = Completer<AppUpdateInfo?>();
  @override
  Future<AppUpdateInfo?> check() => response.future;
}

class OrderEditor extends StatefulWidget {
  const OrderEditor({super.key});
  @override
  State<OrderEditor> createState() => _OrderEditorState();
}

class _OrderEditorState extends State<OrderEditor> {
  VoidCallback? release;
  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    release ??= AppUpdateGate.defer(context);
  }

  @override
  void dispose() {
    release?.call();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => const Text('Pedido en curso');
}

void main() {
  testWidgets(
    'an update arriving during an order waits until the editor closes',
    (tester) async {
      final service = PendingUpdateService();
      addTearDown(service.dispose);
      final editing = ValueNotifier(true);
      addTearDown(editing.dispose);
      await tester.pumpWidget(
        MaterialApp(
          home: AppUpdateGate(
            service: service,
            child: ValueListenableBuilder<bool>(
              valueListenable: editing,
              builder: (_, active, _) =>
                  active ? const OrderEditor() : const Text('Mesas'),
            ),
          ),
        ),
      );
      service.response.complete(
        AppUpdateInfo(
          versionCode: 28,
          versionName: '1.0.27',
          required: false,
          apkUri: Uri.parse('https://example.com/app.apk'),
          sha256Hash: 'a' * 64,
          message: 'Nueva versión',
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Pedido en curso'), findsOneWidget);
      expect(find.text('Actualizar ahora'), findsNothing);
      editing.value = false;
      await tester.pumpAndSettle();
      expect(find.text('Actualizar ahora'), findsOneWidget);
      await tester.tap(find.text('Mas tarde'));
      await tester.pumpAndSettle();
      expect(find.text('Actualizar ahora'), findsNothing);
      expect(find.text('Mesas'), findsOneWidget);
    },
  );

  testWidgets('a failed version check leaves the app usable', (tester) async {
    final service = PendingUpdateService();
    addTearDown(service.dispose);
    await tester.pumpWidget(
      MaterialApp(
        home: AppUpdateGate(service: service, child: const Text('Mesas')),
      ),
    );
    service.response.completeError(StateError('Sin conexión'));
    await tester.pumpAndSettle();
    expect(find.text('Mesas'), findsOneWidget);
    expect(find.text('Actualizar ahora'), findsNothing);
    expect(tester.takeException(), isNull);
  });
}
