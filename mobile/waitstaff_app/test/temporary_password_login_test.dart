import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/auth/data/models/auth_models.dart';
import 'package:waitstaff_app/features/auth/data/services/auth_api_service.dart';

void main() {
  test(
    'temporary password login is not returned as a usable app session',
    () async {
      final dio = Dio();
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (request, handler) {
            handler.resolve(
              Response(
                requestOptions: request,
                data: {'mustChangePassword': true},
              ),
            );
          },
        ),
      );
      addTearDown(dio.close);
      await expectLater(
        AuthApiService(dio).login(
          const LoginRequest(
            email: 'checks@example.invalid',
            password: 'Temporary-checks-123',
          ),
        ),
        throwsA(
          isA<DioException>()
              .having((error) => error.response?.statusCode, 'status', 403)
              .having(
                (error) => error.response?.data['message'],
                'message',
                contains('web'),
              ),
        ),
      );
    },
  );
}
