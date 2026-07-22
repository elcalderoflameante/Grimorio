import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_client.dart' show dioProvider;
import '../models/table_service_models.dart';

class TableServiceApiService {
  TableServiceApiService(this._ref);

  final Ref _ref;

  Future<List<TableServiceRequest>> getRequests() async {
    final dio = _ref.read(dioProvider);
    final response = await dio.get('/TableService/requests');
    final list = response.data as List<dynamic>;
    return list
        .map((e) => TableServiceRequest.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<TableServiceRequest> takeRequest(String id) async {
    final dio = _ref.read(dioProvider);
    final response = await dio.post('/TableService/requests/$id/take');
    return TableServiceRequest.fromJson(response.data as Map<String, dynamic>);
  }

  Future<TableServiceRequest> setStatus(
    String id,
    TableServiceRequestStatus status,
  ) async {
    final dio = _ref.read(dioProvider);
    final response = await dio.post(
      '/TableService/requests/$id/status',
      data: {'status': status.value},
    );
    return TableServiceRequest.fromJson(response.data as Map<String, dynamic>);
  }
}

final tableServiceApiServiceProvider = Provider<TableServiceApiService>(
  (ref) => TableServiceApiService(ref),
);
