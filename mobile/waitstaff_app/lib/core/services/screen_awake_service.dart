import 'package:flutter/services.dart';

class ScreenAwakeService {
  static const MethodChannel _channel = MethodChannel('grimorio/screen_awake');

  Future<void> setKeepScreenOn(bool enabled) async {
    await _channel.invokeMethod<void>('setKeepScreenOn', {'enabled': enabled});
  }
}
