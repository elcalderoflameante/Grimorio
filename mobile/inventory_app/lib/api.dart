import 'dart:convert';
import 'package:http/http.dart' as http;

typedef Json = Map<String, dynamic>;

class InventoryApi {
  InventoryApi({http.Client? client}) : _client = client ?? http.Client();
  final http.Client _client;
  static const baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://erp.elcalderoflameante.com/api',
  );
  String? token;
  void Function()? onExpired;

  Future<dynamic> request(String method, String path, [Json? body]) async {
    final uri = Uri.parse('$baseUrl$path');
    final headers = <String, String>{
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
    final response = await (switch (method) {
      'POST' => _client.post(
        uri,
        headers: headers,
        body: jsonEncode(body ?? {}),
      ),
      'PUT' => _client.put(uri, headers: headers, body: jsonEncode(body ?? {})),
      _ => _client.get(uri, headers: headers),
    }).timeout(const Duration(seconds: 25));
    dynamic data;
    try {
      data = jsonDecode(response.body);
    } catch (_) {
      data = null;
    }
    if (response.statusCode == 401 && token != null) {
      token = null;
      onExpired?.call();
      throw Exception('Tu sesión venció. Ingresa nuevamente con tu PIN.');
    }
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
        data is Map && data['message'] is String
            ? data['message']
            : response.statusCode == 429
            ? 'Demasiados intentos. Espera un minuto e intenta nuevamente.'
            : 'No se pudo completar la operación (${response.statusCode}).',
      );
    }
    if (data == null) throw Exception('Respuesta del servidor no válida.');
    return data;
  }

  void close() => _client.close();
}

List<Json> rows(dynamic data) => (data as List).cast<Json>();
String errorText(Object error) =>
    error.toString().replaceFirst('Exception: ', '');
