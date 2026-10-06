import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:grimorio_voice/grimorio_voice.dart';
import 'package:grimorio_voice/voice_controls.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  final messenger =
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;

  setUp(() {
    messenger.setMockMethodCallHandler(VoiceControls.channel, (call) async {
      if (call.method == 'settings') {
        return <String, Object>{
          'volume': false,
          'sounds': true,
          'accessibility': false,
        };
      }
      return null;
    });
  });

  tearDown(() {
    messenger.setMockMethodCallHandler(VoiceControls.channel, null);
  });

  testWidgets('receive-only mode never exposes transmission controls', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: VoiceShell(
            apiBaseUrl: 'https://example.test/api',
            readToken: () async => 'token',
            canTransmit: false,
            child: const Text('KDS'),
          ),
        ),
      ),
    );

    expect(find.text('KDS'), findsOneWidget);
    expect(find.text('Mantén para hablar'), findsNothing);
    expect(find.byTooltip('Botón y sonidos del walkie'), findsNothing);
    expect(find.byTooltip('Activar recepción de voz'), findsOneWidget);
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(seconds: 2));
  });

  testWidgets('default mode preserves waitstaff transmission controls', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: VoiceShell(
            apiBaseUrl: 'https://example.test/api',
            readToken: () async => 'token',
            child: const Text('Meseros'),
          ),
        ),
      ),
    );

    expect(find.byTooltip('Botón y sonidos del walkie'), findsOneWidget);
    expect(find.byTooltip('Activar walkie-talkie'), findsOneWidget);
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(seconds: 2));
  });
}
