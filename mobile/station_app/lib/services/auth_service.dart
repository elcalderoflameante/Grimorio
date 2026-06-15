import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../core/api_config.dart';

class AuthService {
  static const _keyToken       = 'auth_token';
  static const _keyStationId   = 'station_id';
  static const _keyStationName = 'station_name';

  String get serverUrl => ApiConfig.baseUrl;

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_keyToken);
  }

  Future<String?> getSavedStationId() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_keyStationId);
  }

  Future<String?> getSavedStationName() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_keyStationName);
  }

  Future<void> saveStation(String id, String name) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyStationId, id);
    await prefs.setString(_keyStationName, name);
  }

  Future<void> clearStation() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_keyStationId);
    await prefs.remove(_keyStationName);
  }

  Future<String> login(String email, String password) async {
    final url = Uri.parse('${ApiConfig.baseUrl}/auth/login');
    final response = await http
        .post(
          url,
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode({'email': email, 'password': password}),
        )
        .timeout(const Duration(seconds: 15));

    if (response.statusCode == 200) {
      final data  = jsonDecode(response.body) as Map<String, dynamic>;
      final token = data['accessToken'] as String?;
      if (token == null) throw Exception('Respuesta inválida del servidor.');
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(_keyToken, token);
      return token;
    } else if (response.statusCode == 401) {
      throw Exception('Credenciales incorrectas.');
    } else {
      throw Exception('Error del servidor (${response.statusCode}).');
    }
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_keyToken);
    await prefs.remove(_keyStationId);
    await prefs.remove(_keyStationName);
  }
}
