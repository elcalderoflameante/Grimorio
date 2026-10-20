import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:flutter/services.dart';
import 'package:http/http.dart' as http;
import 'package:package_info_plus/package_info_plus.dart';
import 'package:path_provider/path_provider.dart';

import '../constants/api_config.dart';

class AppUpdateInfo {
  final int versionCode;
  final String versionName;
  final bool required;
  final Uri apkUri;
  final String sha256Hash;
  final String message;

  const AppUpdateInfo({
    required this.versionCode,
    required this.versionName,
    required this.required,
    required this.apkUri,
    required this.sha256Hash,
    required this.message,
  });

  factory AppUpdateInfo.fromJson(Map<String, dynamic> json, Uri manifestUri) {
    final versionCode = (json['versionCode'] as num?)?.toInt();
    final apkUrl = json['apkUrl'] as String?;
    final hash = (json['sha256'] as String? ?? '').trim().toLowerCase();
    if (versionCode == null || versionCode < 1) {
      throw const FormatException('versionCode invalido.');
    }
    if (apkUrl == null || apkUrl.trim().isEmpty) {
      throw const FormatException('apkUrl no configurado.');
    }
    if (!RegExp(r'^[a-f0-9]{64}$').hasMatch(hash)) {
      throw const FormatException('sha256 invalido.');
    }

    final apkUri = manifestUri.resolve(apkUrl.trim());
    if (!['http', 'https'].contains(apkUri.scheme) ||
        apkUri.origin != manifestUri.origin ||
        apkUri.userInfo.isNotEmpty ||
        apkUri.fragment.isNotEmpty ||
        !apkUri.path.endsWith('.apk')) {
      throw const FormatException('La APK debe pertenecer al mismo servidor.');
    }

    return AppUpdateInfo(
      versionCode: versionCode,
      versionName: json['versionName'] as String? ?? versionCode.toString(),
      required: json['required'] as bool? ?? true,
      apkUri: apkUri,
      sha256Hash: hash,
      message: json['message'] as String? ?? '',
    );
  }
}

enum InstallRequestResult { launched, permissionRequired }

class AppUpdateService {
  static const _installer = MethodChannel('com.grimorio.waitstaff_app/updater');
  final http.Client _client;

  AppUpdateService({
    http.Client? client,
    Uri? manifestUri,
    Future<Directory> Function()? temporaryDirectory,
  }) : _client = client ?? http.Client(),
       _manifestUri = manifestUri,
       _temporaryDirectory = temporaryDirectory ?? getTemporaryDirectory;

  final Uri? _manifestUri;
  final Future<Directory> Function() _temporaryDirectory;

  Uri get manifestUri =>
      _manifestUri ??
      Uri.parse('${ApiConfig.hubBaseUrl}/downloads/waitstaff-version.json');

  Future<AppUpdateInfo?> check() async {
    final packageInfo = await PackageInfo.fromPlatform();
    final currentBuild = int.tryParse(packageInfo.buildNumber) ?? 0;
    final uri = manifestUri.replace(
      queryParameters: {
        ...manifestUri.queryParameters,
        't': DateTime.now().millisecondsSinceEpoch.toString(),
      },
    );
    final response = await _client
        .get(uri, headers: const {'Cache-Control': 'no-cache'})
        .timeout(const Duration(seconds: 12));
    if (response.statusCode != 200) {
      throw HttpException(
        'No se pudo consultar la actualizacion (${response.statusCode}).',
      );
    }
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final update = AppUpdateInfo.fromJson(data, manifestUri);
    return update.versionCode > currentBuild ? update : null;
  }

  Future<File> download(
    AppUpdateInfo update, {
    required void Function(double progress) onProgress,
  }) async {
    final directory = await _temporaryDirectory();
    final file = File('${directory.path}/updates/grimorio-meseros-update.apk');
    await file.parent.create(recursive: true);
    if (await file.exists()) await file.delete();

    final request = http.Request('GET', update.apkUri)
      ..headers['Cache-Control'] = 'no-cache';
    final response = await _client
        .send(request)
        .timeout(const Duration(seconds: 20));
    if (response.statusCode != 200) {
      throw HttpException(
        'No se pudo descargar la actualizacion (${response.statusCode}).',
      );
    }

    final sink = file.openWrite();
    var received = 0;
    try {
      await for (final chunk in response.stream.timeout(
        const Duration(seconds: 30),
      )) {
        sink.add(chunk);
        received += chunk.length;
        if (response.contentLength != null && response.contentLength! > 0) {
          onProgress(received / response.contentLength!);
        }
      }
    } catch (_) {
      await sink.close();
      if (await file.exists()) await file.delete();
      rethrow;
    }
    await sink.close();

    final digest = await sha256.bind(file.openRead()).first;
    if (digest.toString().toLowerCase() != update.sha256Hash) {
      await file.delete();
      throw const FormatException(
        'La descarga no coincide con la version publicada.',
      );
    }
    onProgress(1);
    return file;
  }

  Future<InstallRequestResult> requestInstall(
    File apk, {
    bool requestPermission = true,
  }) async {
    final result = await _installer.invokeMethod<String>('installApk', {
      'path': apk.path,
      'requestPermission': requestPermission,
    });
    return result == 'permissionRequired'
        ? InstallRequestResult.permissionRequired
        : InstallRequestResult.launched;
  }

  void dispose() => _client.close();
}
