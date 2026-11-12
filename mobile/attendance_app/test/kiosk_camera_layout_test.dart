import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:grimorio_attendance/core/theme/app_theme.dart';
import 'package:grimorio_attendance/features/kiosk/presentation/widgets/ecuador_clock.dart';
import 'package:grimorio_attendance/features/kiosk/presentation/widgets/kiosk_camera_layout.dart';

void main() {
  const sizes = [
    Size(568, 320),
    Size(640, 360),
    Size(844, 390),
    Size(1024, 600),
    Size(1280, 800),
    Size(320, 568),
    Size(390, 844),
  ];

  Future<void> showLayout(
    WidgetTester tester,
    Size size, {
    double textScale = 1,
    bool pending = true,
    bool unconfirmed = false,
    bool marking = false,
    bool canStartBreak = true,
    bool recovering = false,
    KioskFeedbackType feedbackType = KioskFeedbackType.neutral,
    double? aspectRatio,
    VoidCallback? onStartBreak,
    VoidCallback? onClockOut,
    VoidCallback? onRetry,
  }) async {
    tester.view.physicalSize = size;
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(context).copyWith(
            textScaler: TextScaler.linear(textScale),
            // Simulate a notch/navigation area, not just an empty emulator.
            padding: const EdgeInsets.fromLTRB(24, 24, 24, 16),
          ),
          child: child!,
        ),
        home: Scaffold(
          appBar: AppBar(
            title: const Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Kiosco de asistencia de El Caldero Flameante',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 16),
                ),
                Text('Versión 1.0.8 (9)', style: TextStyle(fontSize: 11)),
              ],
            ),
            actions: [
              const EcuadorClock(),
              IconButton(
                onPressed: () {},
                icon: const Icon(Icons.volume_up_outlined),
              ),
              IconButton(onPressed: () {}, icon: const Icon(Icons.link_off)),
            ],
          ),
          body: KioskCameraLayout(
            preview: const ColoredBox(color: Colors.blueGrey),
            previewAspectRatio:
                aspectRatio ?? (size.width > size.height ? 4 / 3 : 3 / 4),
            message: unconfirmed
                ? 'No se pudo confirmar la última marcación. Reintenta la conexión.'
                : 'María Fernanda, selecciona la marcación',
            statusColor: Colors.blue,
            feedbackType: feedbackType,
            showMarkActions: pending,
            marking: marking,
            canStartBreak: canStartBreak,
            unconfirmedMark: unconfirmed,
            recoveringMark: recovering,
            onStartBreak: onStartBreak ?? () {},
            onClockOut: onClockOut ?? () {},
            onRetry: onRetry ?? () {},
          ),
        ),
      ),
    );
    await tester.pump();
  }

  for (final size in sizes) {
    for (final scale in [1.0, 1.8]) {
      testWidgets('cámara y controles sin solaparse en $size, texto $scale', (
        tester,
      ) async {
        await showLayout(tester, size, textScale: scale);
        final frame = tester.getRect(
          find.byKey(const ValueKey('kiosk-camera-frame')),
        );
        final panel = tester.getRect(
          find.byKey(const ValueKey('kiosk-status-panel')),
        );
        final guide = tester.getRect(
          find.byKey(const ValueKey('kiosk-face-guide')),
        );
        expect(frame.overlaps(panel), isFalse);
        expect(frame.contains(guide.topLeft), isTrue);
        expect(frame.contains(guide.bottomRight), isTrue);
        expect(
          frame.width / frame.height,
          closeTo(size.width > size.height ? 4 / 3 : 3 / 4, 0.001),
        );
        expect(guide.height / guide.width, closeTo(1.3, 0.001));
        if (size.width > size.height) {
          expect(frame.right, lessThanOrEqualTo(panel.left));
        } else {
          expect(frame.bottom, lessThanOrEqualTo(panel.top));
        }
        expect(tester.takeException(), isNull);
        await tester.pumpWidget(const SizedBox.shrink());
      });
    }
  }

  testWidgets('ambas marcaciones accesibles en teléfono horizontal pequeño', (
    tester,
  ) async {
    var breaks = 0;
    var exits = 0;
    await showLayout(
      tester,
      const Size(568, 320),
      textScale: 1.8,
      onStartBreak: () => breaks++,
      onClockOut: () => exits++,
    );
    final startBreak = find.text('Iniciar descanso');
    await tester.ensureVisible(startBreak);
    await tester.pumpAndSettle();
    await tester.tap(startBreak);
    final clockOut = find.text('Registrar salida');
    await tester.ensureVisible(clockOut);
    await tester.pumpAndSettle();
    await tester.tap(clockOut);
    expect(breaks, 1);
    expect(exits, 1);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('conserva una guía suficiente para cámaras panorámicas 16:9', (
    tester,
  ) async {
    await showLayout(tester, const Size(640, 360), aspectRatio: 16 / 9);
    final frame = tester.getRect(
      find.byKey(const ValueKey('kiosk-camera-frame')),
    );
    final guide = tester.getRect(
      find.byKey(const ValueKey('kiosk-face-guide')),
    );
    expect(frame.width / frame.height, closeTo(16 / 9, 0.001));
    // Leave room around a face above the detector's 28% minimum width.
    expect(guide.width / frame.width, greaterThan(0.34));
    expect(guide.height, lessThan(frame.height));
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('reintento accesible y deshabilitado mientras se recupera', (
    tester,
  ) async {
    var retries = 0;
    await showLayout(
      tester,
      const Size(568, 320),
      textScale: 1.8,
      pending: false,
      unconfirmed: true,
      onRetry: () => retries++,
    );
    await tester.ensureVisible(find.text('Reintentar conexión'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Reintentar conexión'));
    expect(retries, 1);
    await showLayout(
      tester,
      const Size(568, 320),
      pending: false,
      unconfirmed: true,
      recovering: true,
    );
    expect(
      tester.widget<FilledButton>(find.byType(FilledButton)).onPressed,
      isNull,
    );
    expect(find.text('Verificando marcación...'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('conserva el bloqueo del descanso usado y de doble tap', (
    tester,
  ) async {
    await showLayout(tester, const Size(640, 360), canStartBreak: false);
    final buttons = tester.widgetList<FilledButton>(find.byType(FilledButton));
    expect(buttons.first.onPressed, isNull);
    expect(buttons.last.onPressed, isNotNull);
    await showLayout(tester, const Size(640, 360), marking: true);
    expect(
      tester
          .widgetList<FilledButton>(find.byType(FilledButton))
          .every((button) => button.onPressed == null),
      isTrue,
    );
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('muestra confirmación y error inequívocos', (tester) async {
    await showLayout(
      tester,
      const Size(640, 360),
      pending: false,
      feedbackType: KioskFeedbackType.success,
    );
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('feedback-success')), findsOneWidget);
    expect(find.byIcon(Icons.check_rounded), findsOneWidget);
    expect(find.bySemanticsLabel('Marcación confirmada'), findsOneWidget);

    await showLayout(
      tester,
      const Size(640, 360),
      pending: false,
      feedbackType: KioskFeedbackType.error,
    );
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('feedback-error')), findsOneWidget);
    expect(find.byIcon(Icons.close_rounded), findsOneWidget);
    expect(
      find.bySemanticsLabel('No se pudo registrar la marcación'),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('reorganiza al girar de vertical a horizontal y regresar', (
    tester,
  ) async {
    for (final size in [
      const Size(360, 640),
      const Size(640, 360),
      const Size(360, 640),
    ]) {
      await showLayout(tester, size);
      expect(find.text('Iniciar descanso'), findsOneWidget);
      expect(find.text('Registrar salida'), findsOneWidget);
      expect(tester.takeException(), isNull);
    }
    await tester.pumpWidget(const SizedBox.shrink());
  });
}
