class ApiConfig {
  static const String _envBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: '',
  );

  static String get baseUrl {
    if (_envBaseUrl.isNotEmpty) return _normalize(_envBaseUrl);
    throw StateError(
      'API_BASE_URL no configurado. Ejecuta con '
      '--dart-define-from-file=dart_define.local.json',
    );
  }

  static String _normalize(String v) =>
      v.endsWith('/') ? v.substring(0, v.length - 1) : v;
}
