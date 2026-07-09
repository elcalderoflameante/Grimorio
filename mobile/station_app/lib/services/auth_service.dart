import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../core/api_config.dart';

class AuthService {
  static const _keyToken       = 'auth_token';
  static const _keyStationId   = 'station_id';
  static const _keyStationName = 'station_name';
  static const _keyStationIds   = 'station_ids';
  static const _keyStationNames = 'station_names';

  String get serverUrl => ApiConfig.baseUrl;

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_keyToken);
  }

  Future<List<String>> getSavedStationIds() async {
    final prefs = await SharedPreferences.getInstance();
    final ids = prefs.getStringList(_keyStationIds);
    if (ids != null && ids.isNotEmpty) return ids;

    final legacyId = prefs.getString(_keyStationId);
    if (legacyId == null || legacyId.isEmpty) return [];
    return [legacyId];
  }

  Future<List<String>> getSavedStationNames() async {
    final prefs = await SharedPreferences.getInstance();
    final names = prefs.getStringList(_keyStationNames);
    if (names != null && names.isNotEmpty) return names;

    final legacyName = prefs.getString(_keyStationName);
    if (legacyName == null || legacyName.isEmpty) return [];
    return [legacyName];
  }

  Future<void> saveStations(List<String> ids, List<String> names) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setStringList(_keyStationIds, ids);
    await prefs.setStringList(_keyStationNames, names);
    if (ids.isNotEmpty) {
      await prefs.setString(_keyStationId, ids.first);
      await prefs.setString(_keyStationName, names.isNotEmpty ? names.first : '');
    } else {
      await prefs.remove(_keyStationId);
      await prefs.remove(_keyStationName);
    }
  }

  Future<void> clearStation() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_keyStationId);
    await prefs.remove(_keyStationName);
    await prefs.remove(_keyStationIds);
    await prefs.remove(_keyStationNames);
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
    await prefs.remove(_keyStationIds);
    await prefs.remove(_keyStationNames);
  }
}
