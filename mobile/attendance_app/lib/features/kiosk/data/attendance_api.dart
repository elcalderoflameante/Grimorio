import 'package:dio/dio.dart';

import '../../../core/network/api_client.dart';
import '../../../core/storage/kiosk_credentials_store.dart';

class IdentifiedEmployee {
  const IdentifiedEmployee({
    required this.id,
    required this.name,
    required this.similarity,
  });

  final String id;
  final String name;
  final double similarity;
}

class AttendanceStatus {
  const AttendanceStatus({
    required this.employeeId,
    required this.employeeName,
    required this.status,
    required this.breakStartedAtUtc,
  });

  final String employeeId;
  final String employeeName;
  final int? status;
  final String? breakStartedAtUtc;

  factory AttendanceStatus.fromJson(Map<String, dynamic> data) =>
      AttendanceStatus(
        employeeId: data['employeeId'].toString(),
        employeeName: data['employeeName'].toString(),
        status: data['status'] as int?,
        breakStartedAtUtc: data['breakStartedAtUtc']?.toString(),
      );
}

class AttendanceApi {
  AttendanceApi() : _client = ApiClient(KioskCredentialsStore());

  final ApiClient _client;

  Future<IdentifiedEmployee> identify(String imagePath) async {
    final response = await _client.dio.post<Map<String, dynamic>>(
      '/attendance/kiosk/identify',
      data: FormData.fromMap({
        'image': await MultipartFile.fromFile(imagePath, filename: 'face.jpg'),
      }),
    );
    final data = response.data!;
    return IdentifiedEmployee(
      id: data['employeeId'].toString(),
      name: data['employeeName'].toString(),
      similarity: (data['similarity'] as num).toDouble(),
    );
  }

  Future<AttendanceStatus> getToday(String employeeId) async {
    final response = await _client.dio.get<Map<String, dynamic>>(
      '/attendance/kiosk/employees/$employeeId/today',
    );
    return AttendanceStatus.fromJson(response.data!);
  }

  Future<AttendanceStatus> mark(String employeeId, String action) async {
    final response = await _client.dio.post<Map<String, dynamic>>(
      '/attendance/kiosk/employees/$employeeId/$action',
      data: const {'method': 1},
    );
    return AttendanceStatus.fromJson(response.data!);
  }
}
