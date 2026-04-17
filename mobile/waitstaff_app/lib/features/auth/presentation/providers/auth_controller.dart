import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/services/push_notification_service.dart';
import '../../data/models/auth_models.dart';
import '../../data/repositories/auth_repository.dart';

final authControllerProvider =
    StateNotifierProvider<AuthController, AuthState>((ref) {
  return AuthController(ref, ref.read(authRepositoryProvider));
});

class AuthState {
  const AuthState({
    required this.isLoading,
    required this.isInitialized,
    required this.session,
    required this.errorMessage,
  });

  final bool isLoading;
  final bool isInitialized;
  final AuthSession? session;
  final String? errorMessage;

  bool get isAuthenticated => session != null && !session!.isExpired;

  AuthState copyWith({
    bool? isLoading,
    bool? isInitialized,
    AuthSession? session,
    String? errorMessage,
    bool clearError = false,
    bool clearSession = false,
  }) {
    return AuthState(
      isLoading: isLoading ?? this.isLoading,
      isInitialized: isInitialized ?? this.isInitialized,
      session: clearSession ? null : (session ?? this.session),
      errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
    );
  }

  static const AuthState initial = AuthState(
    isLoading: false,
    isInitialized: false,
    session: null,
    errorMessage: null,
  );
}

class AuthController extends StateNotifier<AuthState> {
  AuthController(this._ref, this._repository) : super(AuthState.initial) {
    initialize();
  }

  final Ref _ref;
  final AuthRepository _repository;

  Future<void> initialize() async {
    final storedSession = await _repository.getStoredSession();

    state = state.copyWith(
      isInitialized: true,
      session: storedSession,
      clearError: true,
    );

    if (storedSession != null && !storedSession.isExpired) {
      await _syncPushToken();
    }
  }

  Future<bool> login({
    required String email,
    required String password,
  }) async {
    state = state.copyWith(isLoading: true, clearError: true);

    try {
      final session = await _repository.login(email: email, password: password);
      state = state.copyWith(
        isLoading: false,
        session: session,
        clearError: true,
      );
      await _syncPushToken();
      return true;
    } on DioException catch (error) {
      final message = _extractErrorMessage(error);
      state = state.copyWith(
        isLoading: false,
        errorMessage: message,
      );
      return false;
    } catch (_) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: 'No se pudo iniciar sesion. Intenta nuevamente.',
      );
      return false;
    }
  }

  Future<void> logout() async {
    await _ref.read(pushNotificationServiceProvider).unregisterCurrentToken();
    await _repository.logout();
    state = state.copyWith(clearSession: true, clearError: true);
  }

  Future<void> _syncPushToken() async {
    try {
      await _ref.read(pushNotificationServiceProvider).syncTokenWithBackend();
    } catch (_) {
      // Push sync should not block login/session restoration.
    }
  }

  String _extractErrorMessage(DioException error) {
    final data = error.response?.data;
    if (data is Map<String, dynamic>) {
      final message = data['message']?.toString();
      if (message != null && message.isNotEmpty) {
        return message;
      }
    }

    if (error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.connectionError) {
      return 'No hay conexion con el servidor. Verifica la URL de API.';
    }

    if (error.response?.statusCode == 401) {
      return 'Credenciales invalidas.';
    }

    return 'Error de autenticacion. Intenta nuevamente.';
  }
}
