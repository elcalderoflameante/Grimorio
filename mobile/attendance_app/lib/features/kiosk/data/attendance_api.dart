import 'package:dio/dio.dart';
import 'dart:convert';
import 'package:uuid/uuid.dart';

import '../../../core/network/api_client.dart';
import '../../../core/storage/kiosk_credentials_store.dart';

class IdentifiedEmployee {
  const IdentifiedEmployee({
    required this.id,
    required this.name,
    required this.similarity,
    required this.recognitionToken,
  });

  final String id;
  final String name;
  final double similarity;
  final String recognitionToken;
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
  AttendanceApi({Dio? dio, KioskCredentialsStore? store})
    : _store = store ?? KioskCredentialsStore() {
    _dio = dio ?? ApiClient(_store).dio;
  }

  final KioskCredentialsStore _store;
  late final Dio _dio;
  Map<String, dynamic>? _pending;
  bool get hasUnconfirmedMark => _pending != null;

  Future<IdentifiedEmployee> identify(String imagePath) async {
    final response = await _dio.post<Map<String, dynamic>>(
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
      recognitionToken: data['recognitionToken'] as String,
    );
  }

  Future<AttendanceStatus> getToday(String employeeId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/attendance/kiosk/employees/$employeeId/today',
    );
    return AttendanceStatus.fromJson(response.data!);
  }

  Future<AttendanceStatus> mark(
    String employeeId,
    String action,
    String recognitionToken,
  ) async {
    if (_pending != null || await _store.readPendingMark() != null) {
      throw StateError('Primero se debe confirmar la marcación pendiente.');
    }
    final pending = <String, dynamic>{
      'employeeId': employeeId,
      'action': action,
      'operationId': const Uuid().v4(),
      'recognitionToken': recognitionToken,
    };
    await _store.savePendingMark(jsonEncode(pending));
    _pending = pending;
    return _sendPending();
  }

  Future<AttendanceStatus?> recoverPendingMark() async {
    final stored = await _store.readPendingMark();
    if (stored == null) {
      _pending = null;
      return null;
    }
    _pending = Map<String, dynamic>.from(jsonDecode(stored) as Map);
    return _sendPending();
  }

  Future<AttendanceStatus> _sendPending() async {
    final pending = _pending!;
    for (var attempt = 0; ; attempt++) {
      try {
        final response = await _dio.post<Map<String, dynamic>>(
          '/attendance/kiosk/employees/${pending['employeeId']}/${pending['action']}',
          data: {
            'method': 1,
            'recognitionToken': pending['recognitionToken'],
            'operationId': pending['operationId'],
          },
        );
        final result = AttendanceStatus.fromJson(response.data!);
        await _store.clearPendingMark();
        _pending = null;
        return result;
      } on DioException catch (error) {
        final status = error.response?.statusCode;
        // Only a definitive 4xx result is safe to discard. Timeouts and 5xx may
        // happen after commit; persist the operation for recovery after restart.
        if (status != null &&
            status >= 400 &&
            status < 500 &&
            status != 408 &&
            status != 429) {
          await _store.clearPendingMark();
          _pending = null;
          rethrow;
        }
        if (attempt >= 1 || error.type == DioExceptionType.cancel) rethrow;
        await Future<void>.delayed(const Duration(milliseconds: 600));
      }
    }
  }
}
