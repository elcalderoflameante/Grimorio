import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:inventory_app/api.dart';

void main() {
  test('PIN login sends branch, user and PIN, without a stock write', () async {
    final api = InventoryApi(
      client: MockClient((request) async {
        expect(request.url.path, '/api/auth/inventory/login');
        expect(jsonDecode(request.body), {
          'branchId': 'branch',
          'userId': 'user',
          'pin': '0123',
        });
        return http.Response('{"accessToken":"token"}', 200);
      }),
    );
    expect(
      (await api.request('POST', '/auth/inventory/login', {
        'branchId': 'branch',
        'userId': 'user',
        'pin': '0123',
      }))['accessToken'],
      'token',
    );
    api.close();
  });
  test('Expired sessions discard token and request PIN login', () async {
    var expired = false;
    final api = InventoryApi(
      client: MockClient((request) async {
        expect(request.headers['Authorization'], 'Bearer old-token');
        return http.Response('{}', 401);
      }),
    )..token = 'old-token';
    api.onExpired = () => expired = true;
    await expectLater(
      api.request('GET', '/inventory/conteos/mis-pendientes'),
      throwsException,
    );
    expect(expired, isTrue);
    expect(api.token, isNull);
    api.close();
  });
  test(
    'A failed save remains a failure and preserves the server message',
    () async {
      final api = InventoryApi(
        client: MockClient(
          (_) async => http.Response(
            '{"message":"Otra persona actualizo este articulo"}',
            400,
          ),
        ),
      );
      await expectLater(
        api.request('PUT', '/inventory/conteos/count/lineas/line', {
          'quantity': 0,
        }),
        throwsA(predicate((e) => e.toString().contains('Otra persona'))),
      );
      api.close();
    },
  );
}
