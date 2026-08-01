import 'package:dio/dio.dart';

import '../config/api_config.dart';
import '../storage/kiosk_credentials_store.dart';

class ApiClient {
  ApiClient(this._credentialsStore)
    : dio = Dio(
        BaseOptions(
          baseUrl: ApiConfig.baseUrl,
          connectTimeout: const Duration(seconds: 10),
          receiveTimeout: const Duration(seconds: 15),
          headers: const {'Content-Type': 'application/json'},
        ),
      ) {
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final credentials = await _credentialsStore.read();
          if (credentials != null) {
            options.headers['X-Grimorio-Kiosk-Id'] = credentials.kioskId;
            options.headers['X-Grimorio-Kiosk-Key'] = credentials.apiKey;
          }
          handler.next(options);
        },
      ),
    );
  }

  final KioskCredentialsStore _credentialsStore;
  final Dio dio;
}
