import 'package:flutter/foundation.dart';

class ApiConfig {
  static const String _envBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: '',
  );

  static const String _webDefaultBaseUrl = '/api';
  static const String _androidEmulatorBaseUrl = 'http://10.0.2.2:5186/api';

  static String get baseUrl {
    if (_envBaseUrl.isNotEmpty) {
      return _normalize(_envBaseUrl);
    }

    if (kIsWeb) {
      // Web should use relative path and rely on Vite/reverse proxy.
      return _webDefaultBaseUrl;
    }

    // Local DX fallback for Android emulator only.
    if (!kReleaseMode && defaultTargetPlatform == TargetPlatform.android) {
      return _androidEmulatorBaseUrl;
    }

    throw StateError(
      'API_BASE_URL is not configured. Run with '
      '--dart-define=API_BASE_URL=http://YOUR_API_HOST:5186/api '
      'or --dart-define-from-file=dart_define.local.json',
    );
  }

  /// Base URL for SignalR hubs, e.g. http://host:5186
  static String get hubBaseUrl {
    final api = baseUrl; // e.g. http://host:5186/api
    if (api.endsWith('/api')) {
      return api.substring(0, api.length - 4);
    }
    return api;
  }

  static String _normalize(String value) {
    if (value.endsWith('/')) {
      return value.substring(0, value.length - 1);
    }
    return value;
  }
}
