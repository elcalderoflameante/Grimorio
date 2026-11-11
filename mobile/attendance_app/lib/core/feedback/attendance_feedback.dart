import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

/// Audio is best-effort: an unavailable speaker must never fail a clocking.
class AttendanceFeedback {
  static const channel = MethodChannel('grimorio.attendance/feedback');

  Future<void> confirmed() => _play('confirmed');
  Future<void> error() => _play('error');
  Future<void> testSound() => _play('test');

  Future<void> _play(String type) async {
    if (kIsWeb || defaultTargetPlatform != TargetPlatform.android) return;
    try {
      await channel.invokeMethod<bool>('play', {'type': type});
    } catch (error) {
      debugPrint('No se pudo reproducir el sonido de asistencia: $error');
    }
  }
}
