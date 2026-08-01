import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uuid/uuid.dart';

class KioskCredentials {
  const KioskCredentials({required this.kioskId, required this.apiKey});
  final String kioskId;
  final String apiKey;
}

class KioskCredentialsStore {
  KioskCredentialsStore([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  static const _kioskIdKey = 'attendance_kiosk_id';
  static const _apiKeyKey = 'attendance_kiosk_api_key';
  static const _deviceIdentifierKey = 'attendance_device_identifier';
  final FlutterSecureStorage _storage;

  Future<KioskCredentials?> read() async {
    final kioskId = await _storage.read(key: _kioskIdKey);
    final apiKey = await _storage.read(key: _apiKeyKey);
    if (kioskId == null || apiKey == null) return null;
    return KioskCredentials(kioskId: kioskId, apiKey: apiKey);
  }

  Future<void> save(KioskCredentials credentials) async {
    await _storage.write(key: _kioskIdKey, value: credentials.kioskId);
    await _storage.write(key: _apiKeyKey, value: credentials.apiKey);
  }

  Future<void> clear() => _storage.deleteAll();

  Future<String> getOrCreateDeviceIdentifier() async {
    final existing = await _storage.read(key: _deviceIdentifierKey);
    if (existing != null) return existing;
    final value = 'ATT-${const Uuid().v4().toUpperCase()}';
    await _storage.write(key: _deviceIdentifierKey, value: value);
    return value;
  }
}
