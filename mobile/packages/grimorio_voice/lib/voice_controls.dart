import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

class VoiceControls {
  static const channel = MethodChannel('grimorio/voice_controls');
  bool get supported =>
      !kIsWeb && defaultTargetPlatform == TargetPlatform.android;
  bool volume = false, sounds = true, accessibility = false;

  void listen(void Function(bool down, int id) onKey) {
    if (!supported) return;
    channel.setMethodCallHandler((call) async {
      if (call.method == 'key') {
        final args = Map<Object?, Object?>.from(call.arguments as Map);
        onKey(args['down'] == true, (args['id'] as num).toInt());
      }
    });
  }

  Future<T?> _invoke<T>(String method, [Map<String, Object>? args]) async {
    if (!supported) return null;
    try {
      return await channel
          .invokeMethod<T>(method, args)
          .timeout(const Duration(seconds: 1));
    } catch (_) {
      return null;
    }
  }

  Future<void> refresh() async {
    final settings = await _invoke<Map>('settings');
    volume = settings?['volume'] == true;
    sounds = settings?['sounds'] != false;
    accessibility = settings?['accessibility'] == true;
  }

  Future<void> save(bool volumeEnabled, bool soundsEnabled) async {
    await _invoke<void>('saveSettings', {
      'volume': volumeEnabled,
      'sounds': soundsEnabled,
    });
    await refresh();
  }

  Future<void> arm(bool connected) async {
    await _invoke<void>('arm', {'connected': connected});
  }

  Future<bool> isHeld(int id) async =>
      await _invoke<bool>('isHeld', {'id': id}) == true;
  Future<void> cancel() async {
    await _invoke<void>('cancel');
  }

  Future<void> tone(String kind) async {
    if (sounds) await _invoke<void>('tone', {'kind': kind});
  }

  Future<void> openAccessibility() async {
    await _invoke<void>('accessibilitySettings');
  }

  void dispose() {
    if (supported) channel.setMethodCallHandler(null);
  }
}
