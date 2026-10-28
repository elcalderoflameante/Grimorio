import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:inventory_app/core/services/app_update_service.dart';

final manifestUri = Uri.parse(
  'https://erp.example.com/downloads/inventory-version.json',
);
Map<String, dynamic> manifest({int version = 28, String? hash, String? url}) =>
    {
      'versionCode': version,
      'versionName': '1.0.27',
      'required': false,
      'apkUrl': url ?? '/downloads/grimorio-inventario.apk',
      'sha256': hash ?? 'a' * 64,
      'message': 'Nueva versiÃ³n',
    };

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    PackageInfo.setMockInitialValues(
      appName: 'Inventario',
      packageName: 'com.grimorio.inventory_app',
      version: '1.0.26',
      buildNumber: '27',
      buildSignature: 'test',
    );
  });

  test(
    'checks newer builds without cache and ignores current or older builds',
    () async {
      var version = 28;
      final service = AppUpdateService(
        manifestUri: manifestUri,
        client: MockClient((request) async {
          expect(request.url.path, '/downloads/inventory-version.json');
          expect(request.url.queryParameters.containsKey('t'), isTrue);
          expect(request.headers['Cache-Control'], 'no-cache');
          return http.Response(jsonEncode(manifest(version: version)), 200);
        }),
      );
      addTearDown(service.dispose);
      expect((await service.check())?.versionCode, 28);
      version = 27;
      expect(await service.check(), isNull);
      version = 26;
      expect(await service.check(), isNull);
    },
  );

  test('rejects external, insecure and invalid download manifests', () {
    for (final url in [
      'https://other.example.com/app.apk',
      'http://erp.example.com/app.apk',
      'file:///tmp/app.apk',
      '/downloads/app.txt',
    ]) {
      expect(
        () => AppUpdateInfo.fromJson(manifest(url: url), manifestUri),
        throwsFormatException,
      );
    }
    expect(
      () => AppUpdateInfo.fromJson(manifest(hash: 'invalid'), manifestUri),
      throwsFormatException,
    );
    expect(
      () => AppUpdateInfo.fromJson(manifest(version: 0), manifestUri),
      throwsFormatException,
    );
    expect(
      AppUpdateInfo.fromJson(manifest(), manifestUri).apkUri,
      Uri.parse('https://erp.example.com/downloads/grimorio-inventario.apk'),
    );
  });

  test('a missing manifest fails without fabricating an update', () async {
    final service = AppUpdateService(
      manifestUri: manifestUri,
      client: MockClient((_) async => http.Response('missing', 404)),
    );
    addTearDown(service.dispose);
    await expectLater(service.check(), throwsA(isA<HttpException>()));
  });

  test(
    'reads UTF-8 manifests including Windows BOM and accented messages',
    () async {
      final data = manifest()..['message'] = 'Actualización de inventario';
      final service = AppUpdateService(
        manifestUri: manifestUri,
        client: MockClient(
          (_) async => http.Response.bytes(
            utf8.encode('\uFEFF${jsonEncode(data)}'),
            200,
          ),
        ),
      );
      addTearDown(service.dispose);
      expect((await service.check())?.message, 'Actualización de inventario');
    },
  );

  test(
    'download verifies the actual bytes and removes a corrupted APK',
    () async {
      final directory = await Directory.systemTemp.createTemp(
        'inventario-updater-test-',
      );
      addTearDown(() => directory.delete(recursive: true));
      final bytes = utf8.encode('apk-test-bytes');
      final service = AppUpdateService(
        manifestUri: manifestUri,
        temporaryDirectory: () async => directory,
        client: MockClient((_) async => http.Response.bytes(bytes, 200)),
      );
      addTearDown(service.dispose);
      var progress = 0.0;
      final file = await service.download(
        AppUpdateInfo.fromJson(
          manifest(hash: sha256.convert(bytes).toString()),
          manifestUri,
        ),
        onProgress: (value) => progress = value,
      );
      expect(await file.readAsBytes(), bytes);
      expect(progress, 1);
      expect(file.parent.path, endsWith('updates'));
      await expectLater(
        service.download(
          AppUpdateInfo.fromJson(manifest(), manifestUri),
          onProgress: (_) {},
        ),
        throwsFormatException,
      );
      expect(await file.exists(), isFalse);
    },
  );

  test(
    'installer preserves the permission flow without reopening settings on return',
    () async {
      const channel = MethodChannel('com.grimorio.inventory_app/updater');
      final calls = <MethodCall>[];
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(channel, (call) async {
            calls.add(call);
            return calls.length == 1 ? 'permissionRequired' : 'launched';
          });
      addTearDown(
        () => TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
            .setMockMethodCallHandler(channel, null),
      );
      final service = AppUpdateService();
      addTearDown(service.dispose);
      final apk = File('cache/updates/inventario.apk');
      expect(
        await service.requestInstall(apk),
        InstallRequestResult.permissionRequired,
      );
      expect(
        await service.requestInstall(apk, requestPermission: false),
        InstallRequestResult.launched,
      );
      expect(calls.first.arguments['requestPermission'], isTrue);
      expect(calls.last.arguments['requestPermission'], isFalse);
    },
  );
}
