import 'dart:io';

import 'package:dio/dio.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../network/api_client.dart';
import 'alert_service.dart';

@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  // Required so Firebase plugins can be used by Android background isolate.
  await Firebase.initializeApp();
}

final pushNotificationServiceProvider = Provider<PushNotificationService>((
  ref,
) {
  return PushNotificationService(
    dio: ref.read(dioProvider),
    alertService: ref.read(alertServiceProvider),
  );
});

class PushNotificationService {
  PushNotificationService({
    required Dio dio,
    required AlertService alertService,
  }) : _dio = dio,
       _alertService = alertService;

  final Dio _dio;
  final AlertService _alertService;

  bool _initialized = false;

  Future<void> initialize() async {
    if (_initialized) {
      return;
    }

    // Ensure channel exists before any background FCM notification is displayed.
    await _alertService.initialize();

    await FirebaseMessaging.instance.requestPermission(
      alert: true,
      badge: true,
      sound: true,
      provisional: false,
    );

    FirebaseMessaging.onMessage.listen(_onForegroundMessage);
    FirebaseMessaging.instance.onTokenRefresh.listen((token) {
      _registerToken(token).catchError((_) {
        // Ignore transient failures; next app open/login will retry.
      });
    });

    _initialized = true;
  }

  Future<void> syncTokenWithBackend() async {
    if (kIsWeb || !Platform.isAndroid) {
      return;
    }

    await initialize();

    final token = await FirebaseMessaging.instance.getToken();
    if (token == null || token.isEmpty) {
      return;
    }

    await _registerToken(token);
  }

  Future<void> unregisterCurrentToken() async {
    if (kIsWeb || !Platform.isAndroid) {
      return;
    }

    final token = await FirebaseMessaging.instance.getToken();
    if (token == null || token.isEmpty) {
      return;
    }

    try {
      await _dio.delete<void>(
        '/TableService/push-token',
        queryParameters: {'token': token},
      );
    } on DioException {
      // Logout should not fail if backend is temporarily unreachable.
    }
  }

  Future<void> _registerToken(String token) async {
    await _dio.post<void>(
      '/TableService/push-token',
      data: {'token': token, 'platform': 'android'},
    );
  }

  void _onForegroundMessage(RemoteMessage message) {
    // Cuando la app está en primer plano, SignalR ya gestiona la alerta
    // completa (notificación + vibración + TTS con el texto correcto).
    // Ignoramos el mensaje FCM para evitar un TTS duplicado con datos crudos
    // del backend (p.ej. "callWaiter" como body).
    debugPrint(
      '[FCM] Foreground message ignored (SignalR handles it): ${message.messageId}',
    );
  }
}
