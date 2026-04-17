import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/auth_models.dart';
import '../services/auth_api_service.dart';
import '../services/auth_storage_service.dart';

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    apiService: ref.read(authApiServiceProvider),
    storageService: ref.read(authStorageServiceProvider),
  );
});

class AuthRepository {
  AuthRepository({
    required AuthApiService apiService,
    required AuthStorageService storageService,
  })  : _apiService = apiService,
        _storageService = storageService;

  final AuthApiService _apiService;
  final AuthStorageService _storageService;

  Future<AuthSession?> getStoredSession() {
    return _storageService.readSession();
  }

  Future<AuthSession> login({
    required String email,
    required String password,
  }) async {
    final session = await _apiService.login(
      LoginRequest(email: email.trim(), password: password),
    );

    await _storageService.saveSession(session);
    return session;
  }

  Future<void> logout() {
    return _storageService.clearSession();
  }
}
