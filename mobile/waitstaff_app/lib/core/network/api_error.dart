import 'package:dio/dio.dart';

String readableApiError(Object error, {String fallback = 'Ocurrió un error'}) {
  if (error is DioException) {
    final data = error.response?.data;
    if (data is Map<String, dynamic>) {
      final message = data['message'] ?? data['detail'] ?? data['title'];
      if (message is String && message.trim().isNotEmpty) return message.trim();
    }
    if (data is String && data.trim().isNotEmpty && data.length < 240) {
      return data.trim();
    }
    return switch (error.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout ||
      DioExceptionType.receiveTimeout =>
        'La conexión tardó demasiado. Intenta nuevamente.',
      DioExceptionType.connectionError =>
        'No se pudo conectar con el servidor.',
      _ => fallback,
    };
  }
  return fallback;
}
