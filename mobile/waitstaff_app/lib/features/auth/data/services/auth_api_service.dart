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

  Future<List<PinBranch>> getWaitstaffBranches() async {
    final response = await _dio.get<List<dynamic>>('/auth/waitstaff/branches');
    return (response.data ?? const [])
        .map((item) => PinBranch.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<List<PinUser>> getWaitstaffUsers(String branchId) async {
    final response = await _dio.get<List<dynamic>>(
      '/auth/waitstaff/users',
      queryParameters: {'branchId': branchId},
    );
    return (response.data ?? const [])
        .map((item) => PinUser.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<AuthSession> loginWithPin({
    required String branchId,
    required String userId,
    required String pin,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/auth/waitstaff/login',
      data: {'branchId': branchId, 'userId': userId, 'pin': pin},
    );
    if (response.data == null) {
      throw const FormatException('Respuesta vacía del servidor');
    }
    return AuthSession.fromJson(response.data!);
  }
}
