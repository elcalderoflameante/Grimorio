import 'package:flutter_test/flutter_test.dart';
import 'package:grimorio_voice/voice_controls.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  final messenger =
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;

  tearDown(
    () => messenger.setMockMethodCallHandler(VoiceControls.channel, null),
  );

  test('missing native bridge never grants a hardware press', () async {
    final controls = VoiceControls();
    expect(await controls.isHeld(1), isFalse);
    await controls.arm(false);
    await controls.cancel();
  });

  test(
    'native release prevents a late press check from allowing capture',
    () async {
      var held = true;
      messenger.setMockMethodCallHandler(VoiceControls.channel, (call) async {
        if (call.method == 'isHeld') return held && call.arguments['id'] == 7;
        if (call.method == 'cancel') held = false;
        return null;
      });
      final controls = VoiceControls();
      expect(await controls.isHeld(7), isTrue);
      await controls.cancel();
      expect(await controls.isHeld(7), isFalse);
    },
  );

  test('disabled cues do not call the audio bridge', () async {
    var toneCalls = 0;
    messenger.setMockMethodCallHandler(VoiceControls.channel, (call) async {
      if (call.method == 'settings')
        return {'volume': true, 'sounds': false, 'accessibility': true};
      if (call.method == 'tone') toneCalls++;
      return null;
    });
    final controls = VoiceControls();
    await controls.refresh();
    await controls.tone('start');
    expect(controls.volume, isTrue);
    expect(controls.accessibility, isTrue);
    expect(toneCalls, 0);
  });
}
