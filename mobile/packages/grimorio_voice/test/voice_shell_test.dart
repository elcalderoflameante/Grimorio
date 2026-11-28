import 'dart:async';

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
    messenger.setMockMethodCallHandler(
      const MethodChannel('flutter.baseflow.com/permissions/methods'),
      null,
    );
    messenger.setMockMethodCallHandler(
      const MethodChannel('flutter_background'),
      null,
    );
  });

  testWidgets(
    'automatic activation requests permissions without opening the microphone',
    (tester) async {
      var permissionRequests = 0;
      var tokenRequests = 0;
      final backgroundCalls = <String>[];
      messenger.setMockMethodCallHandler(
        const MethodChannel('flutter.baseflow.com/permissions/methods'),
        (call) async {
          if (call.method == 'requestPermissions') {
            permissionRequests++;
            return {
              for (final permission in call.arguments as List) permission: 0,
            };
          }
          return 0;
        },
      );
      messenger.setMockMethodCallHandler(
        const MethodChannel('flutter_background'),
        (call) async {
          backgroundCalls.add(call.method);
          return true;
        },
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: VoiceShell(
              apiBaseUrl: 'https://example.test/api',
              readToken: () async {
                tokenRequests++;
                return 'token';
              },
              autoActivate: true,
              defaultVolumeButtonEnabled: true,
              child: const Text('Meseros'),
            ),
          ),
        ),
      );
      await tester.pump();
      await tester.pump();
      expect(permissionRequests, 1);
      expect(tokenRequests, 0);
      expect(backgroundCalls, isEmpty);
      expect(
        find.text('Permite el micrófono para usar el walkie-talkie.'),
        findsOneWidget,
      );
      expect(find.text('Hablando…'), findsNothing);
      await tester.pumpWidget(const SizedBox.shrink());
      await tester.pump(const Duration(seconds: 2));
    },
  );

  testWidgets(
    'automatic voice starts background service and retries offline without transmitting',
    (tester) async {
      var tokenRequests = 0;
      final backgroundCalls = <String>[];
      messenger.setMockMethodCallHandler(
        const MethodChannel('flutter.baseflow.com/permissions/methods'),
        (call) async {
          if (call.method == 'requestPermissions') {
            return {
              for (final permission in call.arguments as List) permission: 1,
            };
          }
          return 1;
        },
      );
      messenger.setMockMethodCallHandler(
        const MethodChannel('flutter_background'),
        (call) async {
          backgroundCalls.add(call.method);
          return true;
        },
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: VoiceShell(
              apiBaseUrl: 'https://example.test/api',
              readToken: () async {
                tokenRequests++;
                throw Exception(
                  'Simulated connection failure before any HTTP request',
                );
              },
              autoActivate: true,
              defaultVolumeButtonEnabled: true,
              child: const Text('Meseros'),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(backgroundCalls, ['initialize', 'enableBackgroundExecution']);
      expect(tokenRequests, 1);
      expect(find.byTooltip('Salir del canal'), findsOneWidget);
      expect(find.text('Reconectando voz…'), findsOneWidget);
      expect(find.text('Hablando…'), findsNothing);

      tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.paused);
      await tester.pump(const Duration(seconds: 5));
      await tester.pumpAndSettle();
      expect(tokenRequests, 2);
      expect(backgroundCalls, isNot(contains('disableBackgroundExecution')));
      expect(find.text('Hablando…'), findsNothing);
      tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
      await tester.pumpWidget(const SizedBox.shrink());
      await tester.pump(const Duration(seconds: 2));
      expect(backgroundCalls.last, 'disableBackgroundExecution');
    },
  );

  testWidgets(
    'old session cleanup cannot stop the replacement background service',
    (tester) async {
      final cleanupGate = Completer<void>();
      var delayDisarm = false;
      final backgroundCalls = <String>[];
      messenger.setMockMethodCallHandler(VoiceControls.channel, (call) async {
        if (call.method == 'settings') {
          return {'volume': true, 'sounds': true, 'accessibility': false};
        }
        if (call.method == 'arm' &&
            call.arguments['connected'] == false &&
            delayDisarm) {
          await cleanupGate.future;
        }
        return null;
      });
      messenger.setMockMethodCallHandler(
        const MethodChannel('flutter.baseflow.com/permissions/methods'),
        (call) async {
          if (call.method == 'requestPermissions') {
            return {
              for (final permission in call.arguments as List) permission: 1,
            };
          }
          return 1;
        },
      );
      messenger.setMockMethodCallHandler(
        const MethodChannel('flutter_background'),
        (call) async {
          backgroundCalls.add(call.method);
          return true;
        },
      );
      Widget shell(String session) => MaterialApp(
        home: Scaffold(
          body: VoiceShell(
            key: ValueKey(session),
            apiBaseUrl: 'https://example.test/api',
            readToken: () async => throw Exception('Simulated offline state'),
            autoActivate: true,
            child: const Text('Meseros'),
          ),
        ),
      );
      await tester.pumpWidget(shell('first'));
      await tester.pumpAndSettle();
      delayDisarm = true;
      await tester.pumpWidget(shell('second'));
      await tester.pumpAndSettle();
      expect(
        backgroundCalls
            .where((call) => call == 'enableBackgroundExecution')
            .length,
        2,
      );
      cleanupGate.complete();
      await tester.pumpAndSettle();
      expect(backgroundCalls, isNot(contains('disableBackgroundExecution')));
      await tester.pumpWidget(const SizedBox.shrink());
      await tester.pump(const Duration(seconds: 2));
      expect(
        backgroundCalls
            .where((call) => call == 'disableBackgroundExecution')
            .length,
        1,
      );
    },
  );

  for (final visible in [true, false]) {
    testWidgets(
      'top system inset is consumed only by a visible voice bar ($visible)',
      (tester) async {
        EdgeInsets? childPadding;
        const bodyKey = ValueKey('page-body');
        await tester.pumpWidget(
          MaterialApp(
            home: MediaQuery(
              data: const MediaQueryData(
                size: Size(800, 600),
                padding: EdgeInsets.only(top: 36, bottom: 24),
                viewPadding: EdgeInsets.only(top: 36, bottom: 24),
              ),
              child: VoiceShell(
                apiBaseUrl: 'https://example.test/api',
                readToken: () async => 'token',
                compact: true,
                showStatusBar: visible,
                child: Builder(
                  builder: (context) {
                    childPadding = MediaQuery.paddingOf(context);
                    return Scaffold(
                      appBar: AppBar(
                        toolbarHeight: 48,
                        title: const Text('Mesas'),
                      ),
                      body: const SizedBox.expand(key: bodyKey),
                    );
                  },
                ),
              ),
            ),
          ),
        );
        await tester.pumpAndSettle();
        expect(childPadding!.top, visible ? 0 : 36);
        expect(childPadding!.bottom, 24);
        expect(tester.getTopLeft(find.byKey(bodyKey)).dy, visible ? 132 : 84);
        if (visible) {
          expect(
            tester.getSize(find.byTooltip('Activar walkie-talkie')).height,
            greaterThanOrEqualTo(48),
          );
        }
        expect(tester.takeException(), isNull);
        await tester.pumpWidget(const SizedBox.shrink());
        await tester.pump(const Duration(seconds: 2));
      },
    );
  }

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
