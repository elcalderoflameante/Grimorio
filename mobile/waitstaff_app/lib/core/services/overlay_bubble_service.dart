import 'dart:io';

import 'package:flutter/services.dart';

class OverlayBubbleService {
  static const MethodChannel _channel = MethodChannel(
    'grimorio/overlay_bubble',
  );

  Future<bool> canDrawOverlays() async {
    if (!Platform.isAndroid) {
      return false;
    }

    final result = await _channel.invokeMethod<bool>('canDrawOverlays');
    return result ?? false;
  }

  Future<void> requestOverlayPermission() async {
    if (!Platform.isAndroid) {
      return;
    }

    await _channel.invokeMethod<void>('requestOverlayPermission');
  }

  Future<void> showBubble() async {
    if (!Platform.isAndroid) {
      return;
    }

    await _channel.invokeMethod<void>('showBubble');
  }

  Future<void> hideBubble() async {
    if (!Platform.isAndroid) {
      return;
    }

    await _channel.invokeMethod<void>('hideBubble');
  }
}
