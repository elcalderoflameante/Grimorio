import 'dart:async';
import 'dart:io';

import 'package:flutter/material.dart';

import '../services/app_update_service.dart';

class AppUpdateGate extends StatefulWidget {
  final Widget child;
  final AppUpdateService? service;

  const AppUpdateGate({super.key, required this.child, this.service});

  @override
  State<AppUpdateGate> createState() => _AppUpdateGateState();
}

class _AppUpdateGateState extends State<AppUpdateGate>
    with WidgetsBindingObserver {
  late final AppUpdateService _service;
  late final bool _ownsService;
  Timer? _timer;
  AppUpdateInfo? _update;
  File? _downloadedApk;
  bool _checking = false;
  bool _busy = false;
  bool _waitingForPermission = false;
  double _progress = 0;
  String? _error;
  int? _dismissedVersion;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _ownsService = widget.service == null;
    _service = widget.service ?? AppUpdateService();
    unawaited(_check());
    _timer = Timer.periodic(const Duration(hours: 6), (_) => _check());
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && _waitingForPermission) {
      _waitingForPermission = false;
      unawaited(_installDownloaded());
    }
  }

  Future<void> _check() async {
    if (_checking || _busy) return;
    _checking = true;
    try {
      final update = await _service.check();
      if (!mounted || update?.versionCode == _dismissedVersion) return;
      setState(() {
        _update = update;
        _downloadedApk = null;
        _progress = 0;
        _error = null;
      });
    } catch (_) {
      // A failed check must never interrupt kitchen operations.
    } finally {
      _checking = false;
    }
  }

  Future<void> _updateNow() async {
    final update = _update;
    if (update == null || _busy) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      _downloadedApk ??= await _service.download(
        update,
        onProgress: (progress) {
          if (mounted) {
            setState(() => _progress = progress.clamp(0.0, 1.0).toDouble());
          }
        },
      );
      await _launchInstaller();
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error =
            'No se pudo actualizar. Verifica la conexion e intenta otra vez.';
        _downloadedApk = null;
        _progress = 0;
      });
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _installDownloaded() async {
    if (_downloadedApk == null || _busy) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await _launchInstaller();
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'No se pudo abrir el instalador. Intenta nuevamente.';
        });
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _launchInstaller() async {
    final result = await _service.requestInstall(_downloadedApk!);
    if (!mounted) return;
    if (result == InstallRequestResult.permissionRequired) {
      setState(() {
        _waitingForPermission = true;
        _error = 'Activa el permiso para instalar y vuelve a Grimorio KDS.';
      });
    }
  }

  void _dismiss() {
    final update = _update;
    if (update == null || update.required) return;
    setState(() {
      _dismissedVersion = update.versionCode;
      _update = null;
    });
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _timer?.cancel();
    if (_ownsService) _service.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final update = _update;
    return Stack(
      children: [
        widget.child,
        if (update != null)
          Positioned.fill(
            child: PopScope(
              canPop: false,
              onPopInvokedWithResult: (didPop, _) {
                if (!didPop) _dismiss();
              },
              child: ColoredBox(
                color: const Color(0xF20A0A14),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 390),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Material(
                        color: const Color(0xFF1C1C3A),
                        borderRadius: BorderRadius.circular(8),
                        child: Padding(
                          padding: const EdgeInsets.all(18),
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              const Icon(
                                Icons.system_update_alt_rounded,
                                color: Color(0xFFE94560),
                                size: 30,
                              ),
                              const SizedBox(height: 10),
                              const Text(
                                'Actualizacion disponible',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                'Grimorio KDS ${update.versionName}',
                                style: const TextStyle(
                                  color: Colors.white70,
                                  fontSize: 11,
                                ),
                              ),
                              if (update.message.trim().isNotEmpty) ...[
                                const SizedBox(height: 8),
                                Text(
                                  update.message,
                                  textAlign: TextAlign.center,
                                  style: const TextStyle(
                                    color: Colors.white60,
                                    fontSize: 10,
                                  ),
                                ),
                              ],
                              if (_busy) ...[
                                const SizedBox(height: 14),
                                LinearProgressIndicator(
                                  value: _progress > 0 ? _progress : null,
                                  color: const Color(0xFFE94560),
                                  backgroundColor: Colors.white12,
                                ),
                                const SizedBox(height: 5),
                                Text(
                                  _progress > 0
                                      ? 'Descargando ${(_progress * 100).round()}%'
                                      : 'Preparando actualizacion...',
                                  style: const TextStyle(
                                    color: Colors.white54,
                                    fontSize: 9,
                                  ),
                                ),
                              ],
                              if (_error != null) ...[
                                const SizedBox(height: 10),
                                Text(
                                  _error!,
                                  textAlign: TextAlign.center,
                                  style: const TextStyle(
                                    color: Colors.orangeAccent,
                                    fontSize: 10,
                                  ),
                                ),
                              ],
                              const SizedBox(height: 14),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  if (!update.required)
                                    TextButton(
                                      onPressed: _busy ? null : _dismiss,
                                      child: const Text('Mas tarde'),
                                    ),
                                  FilledButton.icon(
                                    onPressed: _busy ? null : _updateNow,
                                    icon: const Icon(
                                      Icons.download_rounded,
                                      size: 17,
                                    ),
                                    label: Text(
                                      _downloadedApk == null
                                          ? 'Actualizar ahora'
                                          : 'Instalar',
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
      ],
    );
  }
}
