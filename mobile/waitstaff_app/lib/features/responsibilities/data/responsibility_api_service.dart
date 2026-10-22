import 'dart:convert';
import 'dart:io';
import 'dart:math';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path_provider/path_provider.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/network/api_client.dart';
import '../../auth/presentation/providers/auth_controller.dart';
import 'responsibility_models.dart';

final responsibilityPendingCountProvider = StateProvider<int>((ref) => 0);
final responsibilityApiProvider = Provider<ResponsibilityApiService>((ref) {
  final session = ref.watch(
    authControllerProvider.select((state) => state.session),
  );
  if (session == null) throw StateError('Inicia sesión para ver tu jornada.');
  final claims =
      jsonDecode(
            utf8.decode(
              base64Url.decode(
                base64Url.normalize(session.accessToken.split('.')[1]),
              ),
            ),
          )
          as Map<String, dynamic>;
  return ResponsibilityApiService(
    ref.read(dioProvider),
    userId: session.userId,
    branchId: claims['BranchId'] as String,
    token: session.accessToken,
  );
});

String responsibilityRequestId() {
  final random = Random.secure();
  final bytes = List.generate(16, (_) => random.nextInt(256));
  bytes[6] = (bytes[6] & 15) | 64;
  bytes[8] = (bytes[8] & 63) | 128;
  final hex = bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
  return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}';
}

class ResponsibilityApiService {
  ResponsibilityApiService(
    this.dio, {
    required this.userId,
    required this.branchId,
    required this.token,
    Future<Directory> Function()? documents,
  }) : _documents = documents ?? getApplicationDocumentsDirectory;
  final Dio dio;
  final String userId, branchId, token;
  final Future<Directory> Function() _documents;
  Future<void>? _sync;
  bool _cameraActive = false;
  static const base = '/operations/responsibilities';
  Options get _options => Options(
    extra: {'skipAuth': true},
    headers: {'Authorization': 'Bearer $token'},
  );

  Future<Directory> _directory() async {
    final safe = RegExp(r'^[a-zA-Z0-9-]+$');
    if (!safe.hasMatch(userId) || !safe.hasMatch(branchId)) {
      throw StateError('Identidad de jornada no válida.');
    }
    final root = await _documents();
    return Directory(
      '${root.path}/responsibilities/$branchId/$userId',
    ).create(recursive: true);
  }

  Future<void> _write(File file, Object value) async {
    final temporary = File('${file.path}.tmp');
    await temporary.writeAsString(jsonEncode(value), flush: true);
    if (await file.exists()) await file.delete();
    await temporary.rename(file.path);
  }

  bool _networkFailure(DioException error) =>
      error.response == null || (error.response?.statusCode ?? 0) >= 500;

  Future<ResponsibilityDay> day() async {
    final file = File('${(await _directory()).path}/day.json');
    try {
      final response = await dio.get<Map<String, dynamic>>(
        '$base/my-day',
        options: _options,
      );
      final json = response.data!;
      await _write(file, json);
      return ResponsibilityDay.fromJson(json);
    } on DioException catch (error) {
      if (!_networkFailure(error) || !await file.exists()) rethrow;
      return ResponsibilityDay.fromJson(
        jsonDecode(await file.readAsString()) as Map<String, dynamic>,
        offline: true,
      );
    }
  }

  Future<ResponsibilityActivity> activity(String id) async {
    final response = await dio.get<Map<String, dynamic>>(
      '$base/activities/$id',
      options: _options,
    );
    return ResponsibilityActivity.fromJson(response.data!);
  }

  Future<ResponsibilityCapture> capture(String activityId, String phase) async {
    final response = await dio.post<Map<String, dynamic>>(
      '$base/activities/$activityId/captures',
      queryParameters: {'phase': phase},
      options: _options,
    );
    return ResponsibilityCapture.fromJson(response.data!);
  }

  Future<void> cancelCapture(String id) async {
    try {
      await dio.delete('$base/captures/$id', options: _options);
    } on DioException {
      /* Ticket expires automatically if offline. */
    }
  }

  Future<void> beginCameraSession(
    ResponsibilityCapture ticket,
    String activityId,
    String phase,
  ) async {
    _cameraActive = true;
    await _write(File('${(await _directory()).path}/camera-session.json'), {
      'captureId': ticket.id,
      'activityId': activityId,
      'phase': phase,
      'issuedAtUtc': ticket.issuedAt.toIso8601String(),
      'expiresAtUtc': ticket.expiresAt.toIso8601String(),
      'localStartedAtUtc': DateTime.now().toUtc().toIso8601String(),
    });
  }

  Future<void> finishCameraSession() async {
    _cameraActive = false;
    final file = File('${(await _directory()).path}/camera-session.json');
    if (await file.exists()) await file.delete();
  }

  /// Android may recreate the app while its external camera is open.
  Future<void> recoverCamera() async {
    if (_cameraActive) return;
    final file = File('${(await _directory()).path}/camera-session.json');
    if (!await file.exists()) return;
    final json = jsonDecode(await file.readAsString()) as Map<String, dynamic>;
    final ticket = ResponsibilityCapture.fromJson({
      'id': json['captureId'],
      'issuedAtUtc': json['issuedAtUtc'],
      'expiresAtUtc': json['expiresAtUtc'],
    });
    var saved = false;
    try {
      final lost = await ImagePicker().retrieveLostData();
      if (lost.exception != null) throw lost.exception!;
      if (lost.files?.isNotEmpty == true) {
        final elapsed = DateTime.now().toUtc().difference(
          DateTime.parse(json['localStartedAtUtc'] as String),
        );
        final capturedAt = ticket.issuedAt.add(elapsed);
        await savePhoto(
          ticket,
          json['activityId'] as String,
          json['phase'] as String,
          File(lost.files!.first.path),
          capturedAt,
        );
        saved = true;
      }
    } finally {
      if (!saved) await cancelCapture(ticket.id);
      await finishCameraSession();
    }
  }

  Future<void> savePhoto(
    ResponsibilityCapture capture,
    String activityId,
    String phase,
    File photo,
    DateTime capturedAt,
  ) async {
    final dir = await _directory();
    final destination = await photo.copy('${dir.path}/${capture.id}.photo');
    await _write(File('${dir.path}/${capture.id}.capture.json'), {
      'captureId': capture.id,
      'activityId': activityId,
      'phase': phase,
      'capturedAtUtc': capturedAt.toUtc().toIso8601String(),
      'fileName': destination.uri.pathSegments.last,
    });
  }

  Future<List<Map<String, dynamic>>> pendingPhotos(String activityId) async {
    final dir = await _directory();
    final result = <Map<String, dynamic>>[];
    await for (final file in dir.list()) {
      if (file is File && file.path.endsWith('.capture.json')) {
        final json =
            jsonDecode(await file.readAsString()) as Map<String, dynamic>;
        if (json['activityId'] == activityId) result.add(json);
      }
    }
    return result;
  }

  Future<bool> hasPendingCompletion(String id) async =>
      File('${(await _directory()).path}/$id.completion.json').exists();
  Future<void> discardPhoto(String captureId) async {
    final dir = await _directory();
    final meta = File('${dir.path}/$captureId.capture.json');
    if (!await meta.exists()) return;
    await cancelCapture(captureId);
    final photo = File('${dir.path}/$captureId.photo');
    if (await photo.exists()) await photo.delete();
    await meta.delete();
  }

  Future<void> complete(
    String id,
    Map<String, String> answers,
    bool hasIssue,
    String notes,
  ) async {
    final file = File('${(await _directory()).path}/$id.completion.json');
    if (!await file.exists()) {
      await _write(file, {
        'idempotencyKey': responsibilityRequestId(),
        'hasIssue': hasIssue,
        'notes': notes,
        'answers': answers.entries
            .map((e) => {'key': e.key, 'value': e.value})
            .toList(),
      });
    }
    await flush();
    // A foreground refresh may have taken its queue snapshot before this save.
    if (await file.exists()) await flush();
    if (await file.exists()) {
      throw StateError(
        'El registro sigue pendiente de confirmación. Reintenta el envío.',
      );
    }
  }

  Future<void> flush() {
    final running = _sync;
    if (running != null) return running;
    return _sync = _flush().whenComplete(() => _sync = null);
  }

  Future<void> _flush() async {
    final dir = await _directory();
    final files = await dir
        .list()
        .where((f) => f is File)
        .cast<File>()
        .toList();
    Object? failure;
    final failedActivities = <String>{};
    for (final file in files.where((f) => f.path.endsWith('.capture.json'))) {
      final json =
          jsonDecode(await file.readAsString()) as Map<String, dynamic>;
      final id = json['activityId'] as String;
      try {
        final current = await activity(id);
        if (!current.evidence.any((e) => e.id == json['captureId'])) {
          await dio.put(
            '$base/captures/${json['captureId']}/photo',
            options: _options.copyWith(contentType: 'multipart/form-data'),
            data: FormData.fromMap({
              'capturedAtUtc': json['capturedAtUtc'],
              'photo': await MultipartFile.fromFile(
                '${dir.path}/${json['captureId']}.photo',
                filename: 'camera.jpg',
              ),
            }),
          );
        }
        final photo = File('${dir.path}/${json['captureId']}.photo');
        if (await photo.exists()) await photo.delete();
        await file.delete();
      } catch (error) {
        failedActivities.add(id);
        failure ??= error;
      }
    }
    for (final file in files.where(
      (f) => f.path.endsWith('.completion.json'),
    )) {
      final name = file.uri.pathSegments.last;
      final id = name.substring(0, name.length - '.completion.json'.length);
      if (failedActivities.contains(id)) continue;
      try {
        final json =
            jsonDecode(await file.readAsString()) as Map<String, dynamic>;
        await dio.post(
          '$base/activities/$id/complete',
          options: _options,
          data: json,
        );
        await file.delete();
      } catch (error) {
        // A definitive rejection is editable again; ambiguous requests remain frozen.
        if (error is DioException && error.response?.statusCode == 400) {
          await file.delete();
        }
        failure ??= error;
      }
    }
    if (failure != null) throw failure;
  }

  Future<List<int>> photo(String evidenceId) async => (await dio.get<List<int>>(
    '$base/evidence/$evidenceId',
    options: _options.copyWith(responseType: ResponseType.bytes),
  )).data!;
}
