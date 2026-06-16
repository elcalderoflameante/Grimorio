import 'dart:async';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:vibration/vibration.dart';

import '../../features/table_requests/data/models/table_service_models.dart';

final alertServiceProvider = Provider<AlertService>((ref) {
  return AlertService();
});

class AlertService {
  static const _channelId = 'table_requests_channel';
  static const _channelName = 'Solicitudes de mesa';
  static const _channelDescription = 'Alertas de nuevas solicitudes de mesa';

  final FlutterLocalNotificationsPlugin _notifications =
      FlutterLocalNotificationsPlugin();
  final FlutterTts _tts = FlutterTts();

  bool _initialized = false;
  Future<void> _speakChain = Future.value();
  DateTime? _lastAlertAt;
  String? _lastAlertSignature;

  Future<void> initialize() async {
    if (_initialized) {
      return;
    }

    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    const settings = InitializationSettings(android: androidSettings);
    await _notifications.initialize(settings);

    final androidNotifications =
        _notifications.resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>();

    await androidNotifications?.createNotificationChannel(
      const AndroidNotificationChannel(
        _channelId,
        _channelName,
        description: _channelDescription,
        importance: Importance.max,
      ),
    );

    await androidNotifications?.requestNotificationsPermission();

    await _tts.setLanguage('es-ES');
    await _tts.setSpeechRate(0.47);
    await _tts.setPitch(1.0);
    await _tts.awaitSpeakCompletion(true);

    _initialized = true;
  }

  Future<void> notifyNewRequest(TableServiceRequest request) async {
    await initialize();

    final table = _normalizeTableName(request);
    final detail = request.ttsDescription;
    final title = 'Nueva solicitud - $table';
    final body = detail;

    if (_shouldThrottle('$title|$body')) {
      return;
    }

    await _notifications.show(
      DateTime.now().millisecondsSinceEpoch ~/ 1000,
      title,
      body,
      const NotificationDetails(
        android: AndroidNotificationDetails(
          _channelId,
          _channelName,
          channelDescription: _channelDescription,
          importance: Importance.max,
          priority: Priority.high,
        ),
      ),
    );

    await _vibrate();
    final ttsText = 'Nueva solicitud en ${_normalizeTableName(request)}, $detail';
    debugPrint('[TTS] type=${request.type.name} | customMessage="${request.customMessage}" | ttsDescription="${request.ttsDescription}" | texto="$ttsText"');
    await _speak(ttsText);
  }

  Future<void> notifyGenericAlert({
    required String title,
    required String body,
    bool speakBody = false,
  }) async {
    await initialize();

    if (_shouldThrottle('$title|$body')) {
      return;
    }

    await _notifications.show(
      DateTime.now().millisecondsSinceEpoch ~/ 1000,
      title,
      body,
      const NotificationDetails(
        android: AndroidNotificationDetails(
          _channelId,
          _channelName,
          channelDescription: _channelDescription,
          importance: Importance.max,
          priority: Priority.high,
        ),
      ),
    );

    await _vibrate();
    if (speakBody) {
      await _speak(body);
    }
  }

  Future<void> _vibrate() async {
    if (kIsWeb || !Platform.isAndroid) {
      return;
    }

    final hasVibrator = await Vibration.hasVibrator();
    if (!hasVibrator) {
      return;
    }

    await Vibration.vibrate(duration: 450, amplitude: 180);
  }

  Future<void> _speak(String message) async {
    if (message.trim().isEmpty) {
      return;
    }

    _speakChain = _speakChain.then((_) async {
      try {
        await _tts.speak(message);
      } catch (e) {
        debugPrint('[AlertService] TTS error: $e');
      }
    });

    await _speakChain;
  }

  String _normalizeTableName(TableServiceRequest request) {
    final tableName = request.tableName.trim();
    if (tableName.isNotEmpty) {
      if (tableName.toLowerCase().startsWith('mesa')) {
        return tableName;
      }
      return 'mesa $tableName';
    }

    final code = request.tableCode.trim();
    if (code.isNotEmpty) {
      return 'mesa $code';
    }

    return 'mesa';
  }

  String _ttsTableLabel(TableServiceRequest request) {
    final tableName = request.tableName.trim();
    if (tableName.isNotEmpty) {
      if (tableName.toLowerCase().startsWith('mesa')) {
        return tableName[0].toUpperCase() + tableName.substring(1);
      }
      return 'Mesa $tableName';
    }

    final code = request.tableCode.trim();
    if (code.isNotEmpty) {
      return 'Mesa $code';
    }

    return 'Mesa';
  }

  bool _shouldThrottle(String signature) {
    final now = DateTime.now();
    if (_lastAlertSignature == signature &&
        _lastAlertAt != null &&
        now.difference(_lastAlertAt!).inSeconds < 3) {
      return true;
    }

    _lastAlertSignature = signature;
    _lastAlertAt = now;
    return false;
  }
}
