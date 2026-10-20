import 'package:flutter_test/flutter_test.dart';
import 'package:station_app/services/app_update_service.dart';

void main() {
  test('parses a valid update manifest and resolves the APK URL', () {
    final update = AppUpdateInfo.fromJson({
      'versionCode': 16,
      'versionName': '1.0.15',
      'required': true,
      'apkUrl': '/downloads/grimorio-estaciones.apk',
      'sha256': 'a' * 64,
      'message': 'Nueva version',
    }, Uri.parse('https://erp.example.com/downloads/station-version.json'));

    expect(update.versionCode, 16);
    expect(update.required, isTrue);
    expect(
      update.apkUri,
      Uri.parse('https://erp.example.com/downloads/grimorio-estaciones.apk'),
    );
  });

  test('rejects a manifest without a valid SHA-256 hash', () {
    expect(
      () => AppUpdateInfo.fromJson({
        'versionCode': 16,
        'apkUrl': '/downloads/grimorio-estaciones.apk',
        'sha256': 'invalid',
      }, Uri.parse('https://erp.example.com/downloads/station-version.json')),
      throwsFormatException,
    );
  });
}
