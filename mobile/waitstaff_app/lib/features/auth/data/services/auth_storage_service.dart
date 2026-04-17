import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/auth_models.dart';

final authStorageServiceProvider = Provider<AuthStorageService>((ref) {
  return const AuthStorageService();
});

class AuthStorageService {
  const AuthStorageService();

  static const _storage = FlutterSecureStorage();

  static const String _accessTokenKey = 'grimorio_access_token';
  static const String _refreshTokenKey = 'grimorio_refresh_token';
  static const String _sessionKey = 'grimorio_session_json';

  Future<void> saveSession(AuthSession session) async {
    await _storage.write(key: _accessTokenKey, value: session.accessToken);
    await _storage.write(key: _refreshTokenKey, value: session.refreshToken);
    await _storage.write(key: _sessionKey, value: jsonEncode(session.toJson()));
  }

  Future<AuthSession?> readSession() async {
    final sessionJson = await _storage.read(key: _sessionKey);
    if (sessionJson == null || sessionJson.isEmpty) {
      return null;
    }

    try {
      final map = jsonDecode(sessionJson) as Map<String, dynamic>;
      final session = AuthSession.fromJson(map);
      if (session.accessToken.isEmpty || session.isExpired) {
        await clearSession();
        return null;
      }
      return session;
    } catch (_) {
      await clearSession();
      return null;
    }
  }

  Future<String?> readAccessToken() async {
    return _storage.read(key: _accessTokenKey);
  }

  Future<void> clearSession() async {
    await _storage.delete(key: _accessTokenKey);
    await _storage.delete(key: _refreshTokenKey);
    await _storage.delete(key: _sessionKey);
  }
}
