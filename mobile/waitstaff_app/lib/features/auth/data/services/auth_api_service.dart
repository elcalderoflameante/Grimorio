import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_client.dart';
import '../models/auth_models.dart';

final authApiServiceProvider = Provider<AuthApiService>((ref) {
  return AuthApiService(ref.read(dioProvider));
});

class AuthApiService {
  AuthApiService(this._dio);

  final Dio _dio;

  Future<AuthSession> login(LoginRequest request) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/auth/login',
      data: request.toJson(),
    );

    final data = response.data;
    if (data == null) {
      throw const FormatException('Respuesta vacia del servidor');
    }

    return AuthSession.fromJson(data);
  }
}
