import 'dart:convert';
import 'package:http/http.dart' as http;
import '../core/api_config.dart';
import '../models/work_station.dart';
import '../models/station_item.dart';

class UnauthorizedException implements Exception {}

class ApiService {
  final String token;

  ApiService({required this.token});

  String get _base => ApiConfig.baseUrl;

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      };

  Future<T> _get<T>(String path, T Function(dynamic body) parse) async {
    final response = await http
        .get(Uri.parse('$_base$path'), headers: _headers)
        .timeout(const Duration(seconds: 15));

    if (response.statusCode == 401) throw UnauthorizedException();
    if (response.statusCode != 200) {
      throw Exception('Error $path (${response.statusCode}): '
          '${response.body.length > 200 ? response.body.substring(0, 200) : response.body}');
    }
    return parse(jsonDecode(response.body));
  }

  Future<List<WorkStation>> getStations() => _get(
        '/pos/estaciones',
        (body) => (body as List)
            .map((e) => WorkStation.fromJson(e as Map<String, dynamic>))
            .where((s) => s.isActive)
            .toList(),
      );

  Future<List<StationItem>> getStationItems(String stationId) => _get(
        '/pos/estaciones/$stationId/items',
        (body) => (body as List)
            .map((e) => StationItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  Future<List<StationItem>> getCompletedStationItems(String stationId) => _get(
        '/pos/estaciones/$stationId/completados',
        (body) => (body as List)
            .map((e) => StationItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  Future<void> updateItemStatus(String orderItemId, String status) async {
    final response = await http
        .patch(
          Uri.parse('$_base/pos/orden-items/$orderItemId/estado'),
          headers: _headers,
          body: jsonEncode({'estado': status}),
        )
        .timeout(const Duration(seconds: 15));

    if (response.statusCode == 401) throw UnauthorizedException();
    if (response.statusCode != 200) {
      throw Exception('Error al actualizar estado (${response.statusCode}).');
    }
  }
}
