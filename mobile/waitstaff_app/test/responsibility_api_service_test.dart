import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/responsibilities/data/responsibility_api_service.dart';
import 'package:waitstaff_app/features/responsibilities/data/responsibility_models.dart';

Map<String, dynamic> activity({List<Map<String, dynamic>> photos = const []}) =>
    {
      'id': 'activity-1',
      'taskName': 'Baño',
      'areaName': 'Primer piso',
      'positionName': 'Mesera 1',
      'instructions': 'Verificar jabón',
      'stage': 'Opening',
      'photoMode': 'After',
      'status': 'Pending',
      'workDate': '2026-10-21',
      'availableAtUtc': '2026-10-21T12:00:00Z',
      'dueAtUtc': '2026-10-21T13:00:00Z',
      'checklist': [],
      'answers': [],
      'evidence': photos,
    };

void main() {
  late Directory root;
  late Dio dio;
  late ResponsibilityApiService service;
  setUp(() async {
    root = await Directory.systemTemp.createTemp('responsibilities-checks-');
    dio = Dio(BaseOptions(baseUrl: 'https://example.invalid/api'));
    service = ResponsibilityApiService(
      dio,
      userId: 'employee-user',
      branchId: 'branch-1',
      token: 'fixed-owner-token',
      documents: () async => root,
    );
  });
  tearDown(() async {
    dio.close();
    await root.delete(recursive: true);
  });

  test(
    'completion added during an active refresh is sent before acknowledgement',
    () async {
      final started = Completer<void>();
      final release = Completer<void>();
      var posted = 0;
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (request, handler) async {
            if (request.path.endsWith('/activity-1')) {
              if (!started.isCompleted) {
                started.complete();
              }
              await release.future;
              handler.resolve(
                Response(requestOptions: request, data: activity()),
              );
            } else {
              if (request.path.endsWith('/complete')) {
                posted++;
              }
              handler.resolve(
                Response(requestOptions: request, data: <String, dynamic>{}),
              );
            }
          },
        ),
      );
      final camera = File('${root.path}/source.jpg');
      await camera.writeAsBytes([1, 2, 3]);
      final capture = ResponsibilityCapture.fromJson({
        'id': 'capture-1',
        'issuedAtUtc': '2026-10-21T12:00:00Z',
        'expiresAtUtc': '2026-10-21T12:30:00Z',
      });
      await service.savePhoto(
        capture,
        'activity-1',
        'After',
        camera,
        DateTime.now().toUtc(),
      );
      final refresh = service.flush();
      await started.future;
      final save = service.complete('activity-1', {}, false, '');
      await Future<void>.delayed(const Duration(milliseconds: 30));
      release.complete();
      await refresh;
      await save;
      expect(posted, 1);
      expect(await service.hasPendingCompletion('activity-1'), isFalse);
    },
  );

  test(
    'offline day cache is isolated by branch and authenticated user',
    () async {
      var offline = false;
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (request, handler) {
            expect(
              request.headers['Authorization'],
              'Bearer fixed-owner-token',
            );
            expect(request.extra['skipAuth'], isTrue);
            if (offline) {
              handler.reject(
                DioException(
                  requestOptions: request,
                  type: DioExceptionType.connectionError,
                ),
              );
            } else {
              handler.resolve(
                Response(
                  requestOptions: request,
                  data: {
                    'workDate': '2026-10-21',
                    'activities': [activity()],
                  },
                ),
              );
            }
          },
        ),
      );
      expect((await service.day()).offline, isFalse);
      offline = true;
      expect((await service.day()).offline, isTrue);
      final other = ResponsibilityApiService(
        dio,
        userId: 'other-user',
        branchId: 'branch-1',
        token: 'fixed-owner-token',
        documents: () async => root,
      );
      await expectLater(other.day(), throwsA(isA<DioException>()));
      final otherBranch = ResponsibilityApiService(
        dio,
        userId: 'employee-user',
        branchId: 'branch-2',
        token: 'fixed-owner-token',
        documents: () async => root,
      );
      await expectLater(otherBranch.day(), throwsA(isA<DioException>()));
    },
  );

  test(
    'ambiguous completion retries reuse the original payload across a restart',
    () async {
      final attempts = <Map<String, dynamic>>[];
      var succeed = false;
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (request, handler) {
            attempts.add(Map<String, dynamic>.from(request.data as Map));
            if (succeed) {
              handler.resolve(
                Response(requestOptions: request, data: activity()),
              );
            } else {
              handler.reject(
                DioException(
                  requestOptions: request,
                  type: DioExceptionType.receiveTimeout,
                ),
              );
            }
          },
        ),
      );
      await expectLater(
        service.complete(
          'activity-1',
          {'soap': 'Agotado'},
          true,
          'Sin reposición',
        ),
        throwsA(isA<DioException>()),
      );
      expect(await service.hasPendingCompletion('activity-1'), isTrue);
      service = ResponsibilityApiService(
        dio,
        userId: 'employee-user',
        branchId: 'branch-1',
        token: 'fixed-owner-token',
        documents: () async => root,
      );
      succeed = true;
      await service.complete(
        'activity-1',
        {'soap': 'Suficiente'},
        false,
        'Changed',
      );
      expect(attempts[1], attempts[0]);
      expect(attempts[0]['notes'], 'Sin reposición');
      expect(
        attempts[0]['idempotencyKey'],
        matches(RegExp(r'^[a-f0-9-]{36}$')),
      );
      expect(await service.hasPendingCompletion('activity-1'), isFalse);
    },
  );

  test(
    'a definitive completion rejection releases the request for correction',
    () async {
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (request, handler) {
            handler.reject(
              DioException(
                requestOptions: request,
                type: DioExceptionType.badResponse,
                response: Response(
                  requestOptions: request,
                  statusCode: 400,
                  data: {'message': 'Falta la foto'},
                ),
              ),
            );
          },
        ),
      );
      await expectLater(
        service.complete('activity-1', {}, false, ''),
        throwsA(isA<DioException>()),
      );
      expect(await service.hasPendingCompletion('activity-1'), isFalse);
    },
  );

  test(
    'photo upload acknowledgement lost does not upload a duplicate',
    () async {
      final capture = ResponsibilityCapture.fromJson({
        'id': 'capture-1',
        'issuedAtUtc': '2026-10-21T12:00:00Z',
        'expiresAtUtc': '2026-10-21T12:30:00Z',
      });
      final source = File('${root.path}/camera.jpg');
      await source.writeAsBytes([1, 2, 3]);
      await service.savePhoto(
        capture,
        'activity-1',
        'After',
        source,
        capture.issuedAt,
      );
      expect(await service.pendingPhotos('activity-1'), hasLength(1));
      final methods = <String>[];
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (request, handler) {
            methods.add(request.method);
            handler.resolve(
              Response(
                requestOptions: request,
                data: activity(
                  photos: [
                    {'id': 'capture-1', 'phase': 'After'},
                  ],
                ),
              ),
            );
          },
        ),
      );
      await service.flush();
      expect(methods, ['GET']);
      expect(await service.pendingPhotos('activity-1'), isEmpty);
    },
  );

  test(
    'a queued completion waits for its photograph to reach the server',
    () async {
      final capture = ResponsibilityCapture.fromJson({
        'id': 'capture-1',
        'issuedAtUtc': '2026-10-21T12:00:00Z',
        'expiresAtUtc': '2026-10-21T12:30:00Z',
      });
      final source = File('${root.path}/camera.jpg');
      await source.writeAsBytes([1, 2, 3]);
      await service.savePhoto(
        capture,
        'activity-1',
        'After',
        source,
        capture.issuedAt,
      );
      final calls = <String>[];
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (request, handler) {
            calls.add(request.method);
            handler.reject(
              DioException(
                requestOptions: request,
                type: DioExceptionType.connectionError,
              ),
            );
          },
        ),
      );
      await expectLater(
        service.complete('activity-1', {}, false, ''),
        throwsA(isA<DioException>()),
      );
      expect(calls, ['GET']);
      expect(await service.hasPendingCompletion('activity-1'), isTrue);
      expect(await service.pendingPhotos('activity-1'), hasLength(1));
    },
  );

  test('parse checklist answers and identify attention options', () {
    final json = activity();
    json['checklist'] = [
      {
        'key': 'soap',
        'label': 'Jabón',
        'options': ['Suficiente', 'Agotado'],
        'attentionOptions': ['Agotado'],
      },
    ];
    json['answers'] = [
      {'key': 'soap', 'value': 'Agotado'},
    ];
    final parsed = ResponsibilityActivity.fromJson(json);
    expect(parsed.checklist.single.attentionOptions, ['Agotado']);
    expect(parsed.answers['soap'], 'Agotado');
    expect(jsonEncode(parsed.answers), contains('Agotado'));
  });
}
