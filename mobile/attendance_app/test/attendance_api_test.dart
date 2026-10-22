import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:grimorio_attendance/core/storage/kiosk_credentials_store.dart';
import 'package:grimorio_attendance/features/kiosk/data/attendance_api.dart';

Map<String, dynamic> result() => {
  'employeeId': 'employee',
  'employeeName': 'Ana',
  'status': 1,
};
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  setUp(() => FlutterSecureStorage.setMockInitialValues({}));

  test('reintenta la misma operación cuando se pierde la respuesta', () async {
    final requests = <Map<String, dynamic>>[];
    final dio = Dio();
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          requests.add(Map<String, dynamic>.from(options.data as Map));
          if (requests.length == 1) {
            handler.reject(
              DioException(
                requestOptions: options,
                type: DioExceptionType.receiveTimeout,
              ),
            );
          } else {
            handler.resolve(
              Response(
                requestOptions: options,
                data: result(),
                statusCode: 200,
              ),
            );
          }
        },
      ),
    );
    final api = AttendanceApi(dio: dio);
    expect((await api.mark('employee', 'clock-in', 'token')).status, 1);
    expect(requests, hasLength(2));
    expect(requests.first['operationId'], requests.last['operationId']);
    expect(await KioskCredentialsStore().readPendingMark(), isNull);
  });

  test(
    'conserva y recupera una marcación después de reiniciar la app',
    () async {
      final dio = Dio();
      String? operationId;
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (options, handler) {
            operationId ??= (options.data as Map)['operationId'] as String;
            handler.reject(
              DioException(
                requestOptions: options,
                type: DioExceptionType.connectionError,
              ),
            );
          },
        ),
      );
      final api = AttendanceApi(dio: dio);
      await expectLater(
        api.mark('employee', 'clock-in', 'token'),
        throwsA(isA<DioException>()),
      );
      expect(api.hasUnconfirmedMark, isTrue);
      await expectLater(
        api.mark('employee', 'clock-in', 'another-token'),
        throwsStateError,
      );
      final recovered = Dio();
      recovered.interceptors.add(
        InterceptorsWrapper(
          onRequest: (options, handler) {
            expect((options.data as Map)['operationId'], operationId);
            expect((options.data as Map)['recognitionToken'], 'token');
            handler.resolve(
              Response(
                requestOptions: options,
                data: result(),
                statusCode: 200,
              ),
            );
          },
        ),
      );
      final restarted = AttendanceApi(dio: recovered);
      expect((await restarted.recoverPendingMark())?.employeeName, 'Ana');
      expect(restarted.hasUnconfirmedMark, isFalse);
      expect(await KioskCredentialsStore().readPendingMark(), isNull);
    },
  );

  test('descarta un rechazo definitivo sin repetir la marcación', () async {
    var calls = 0;
    final dio = Dio();
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          calls++;
          handler.reject(
            DioException(
              requestOptions: options,
              response: Response(requestOptions: options, statusCode: 409),
            ),
          );
        },
      ),
    );
    final api = AttendanceApi(dio: dio);
    await expectLater(
      api.mark('employee', 'clock-in', 'expired'),
      throwsA(isA<DioException>()),
    );
    expect(calls, 1);
    expect(api.hasUnconfirmedMark, isFalse);
    expect(await KioskCredentialsStore().readPendingMark(), isNull);
  });

  test(
    'conserva claves existentes y elimina pendientes al desvincular',
    () async {
      FlutterSecureStorage.setMockInitialValues({
        'attendance_kiosk_id': 'kiosk',
        'attendance_kiosk_api_key': 'secret',
        'attendance_device_identifier': 'ATT-existing',
      });
      final store = KioskCredentialsStore();
      expect((await store.read())?.apiKey, 'secret');
      expect(await store.getOrCreateDeviceIdentifier(), 'ATT-existing');
      await store.savePendingMark('{}');
      await store.clearCredentials();
      expect(await store.read(), isNull);
      expect(await store.readPendingMark(), isNull);
      expect(await store.getOrCreateDeviceIdentifier(), 'ATT-existing');
    },
  );
}
