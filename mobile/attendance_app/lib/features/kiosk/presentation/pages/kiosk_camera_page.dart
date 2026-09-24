import 'dart:async';
import 'dart:io';
import 'package:camera/camera.dart';
import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import '../../../biometrics/data/face_detection_service.dart';
import '../../../biometrics/domain/active_liveness_challenge.dart';
import '../../../biometrics/domain/face_detection_result.dart';
import '../../data/attendance_api.dart';

class KioskCameraPage extends StatefulWidget {
  const KioskCameraPage({
    super.key,
    required this.kioskName,
    required this.onUnlink,
  });
  final String kioskName;
  final Future<void> Function() onUnlink;

  @override
  State<KioskCameraPage> createState() => _KioskCameraPageState();
}

class _KioskCameraPageState extends State<KioskCameraPage>
    with WidgetsBindingObserver {
  final _faceDetection = FaceDetectionService();
  final _attendanceApi = AttendanceApi();
  ActiveLivenessChallenge _liveness = ActiveLivenessChallenge();
  CameraController? _camera;
  Timer? _scanTimer;
  Timer? _selectionTimer;
  bool _active = true;
  int _session = 0;
  bool _awaitingDeparture = false;
  int _emptyFrames = 0;
  bool _canStartBreak = true;
  bool _processing = false;
  bool _initializingCamera = false;
  bool _coolingDown = false;
  IdentifiedEmployee? _pendingEmployee;
  bool _marking = false;
  String _message = 'Acércate a la cámara';
  Color _statusColor = const Color(0xFF1890FF);

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _initializeCamera();
  }

  Future<void> _initializeCamera() async {
    if (!mounted || !_active || _initializingCamera || _camera != null) return;
    _initializingCamera = true;
    CameraController? initializingController;
    try {
      final available = await availableCameras();
      if (available.isEmpty) {
        throw CameraException(
          'camera_not_found',
          'No se encontró una cámara disponible.',
        );
      }
      final selected = available.firstWhere(
        (item) => item.lensDirection == CameraLensDirection.front,
        orElse: () => available.first,
      );
      final controller = CameraController(
        selected,
        ResolutionPreset.medium,
        enableAudio: false,
      );
      initializingController = controller;
      await controller.initialize();
      if (!mounted || !_active) {
        await controller.dispose();
        return;
      }
      setState(() => _camera = controller);
      _scanTimer = Timer.periodic(
        const Duration(milliseconds: 500),
        (_) => _inspectFrame(),
      );
    } catch (error, stackTrace) {
      await initializingController?.dispose();
      debugPrint('No se pudo iniciar la cámara: $error\n$stackTrace');
      if (mounted) {
        setState(() {
          _statusColor = Colors.redAccent;
          _message = 'No se pudo iniciar la cámara frontal';
        });
      }
    } finally {
      if (mounted) setState(() => _initializingCamera = false);
    }
  }

  Future<void> _inspectFrame() async {
    final camera = _camera;
    if (!_active ||
        _pendingEmployee != null ||
        _processing ||
        _coolingDown ||
        camera == null ||
        !camera.value.isInitialized ||
        camera.value.isTakingPicture) {
      return;
    }
    _processing = true;
    XFile? capture;
    try {
      capture = await camera.takePicture();
      final result = await _faceDetection.inspectFile(capture.path);
      if (!mounted || !_active || camera != _camera) return;
      if (_awaitingDeparture) {
        _emptyFrames = result.issues.contains(FaceQualityIssue.noFace)
            ? _emptyFrames + 1
            : 0;
        if (_emptyFrames >= 2) {
          _awaitingDeparture = false;
          _liveness.reset();
          setState(() => _message = _liveness.instruction);
        }
        return;
      }
      _liveness.process(result);
      if (_liveness.isCompleted) {
        setState(() {
          _statusColor = const Color(0xFF52C41A);
          _message = 'Identificando...';
        });
        _scanTimer?.cancel();
        await _identify(capture.path);
      } else if (_liveness.step != LivenessStep.center || result.isAcceptable) {
        setState(() {
          _statusColor = const Color(0xFF1890FF);
          _message = _liveness.instruction;
        });
      } else {
        setState(() {
          _statusColor = Colors.orange;
          _message = _messageFor(result.issues.first);
        });
      }
    } catch (error, stackTrace) {
      debugPrint('Error durante detección facial: $error\n$stackTrace');
      if (mounted) {
        setState(() {
          _statusColor = Colors.redAccent;
          _message = 'No se pudo analizar la imagen. Reintentando...';
        });
      }
    } finally {
      if (capture != null) {
        try {
          await File(capture.path).delete();
        } catch (_) {}
      }
      _processing = false;
    }
  }

  Future<void> _identify(String imagePath) async {
    final session = _session;
    var shouldCooldown = true;
    try {
      final employee = await _attendanceApi.identify(imagePath);
      if (!mounted || !_active || session != _session) return;
      final status = await _attendanceApi.getToday(employee.id);
      if (!mounted || !_active || session != _session) return;

      if (status.status == null) {
        await _performMark(employee, 'clock-in', 'Entrada registrada');
      } else if (status.status == 2) {
        await _performMark(employee, 'break/end', 'Fin de descanso registrado');
      } else if (status.status == 1) {
        shouldCooldown = false;
        setState(() {
          _pendingEmployee = employee;
          _canStartBreak = status.breakStartedAtUtc == null;
          _statusColor = const Color(0xFF1890FF);
          _message = '${employee.name}, selecciona la marcación';
        });
        _selectionTimer?.cancel();
        _selectionTimer = Timer(const Duration(seconds: 10), () {
          if (mounted && !_marking) {
            setState(() => _pendingEmployee = null);
            unawaited(_startCooldown());
          }
        });
      } else {
        setState(() {
          _statusColor = Colors.orange;
          _message = '${employee.name}, tu jornada ya fue finalizada';
        });
      }
    } on DioException catch (error) {
      if (!mounted) return;
      if (error.response?.statusCode == 401) {
        await widget.onUnlink();
        return;
      }
      final data = error.response?.data;
      final serverMessage = data is Map ? data['message']?.toString() : null;
      setState(() {
        _statusColor = Colors.redAccent;
        _message = serverMessage ?? 'No se pudo identificar el rostro';
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _statusColor = Colors.redAccent;
        _message = 'No se pudo identificar el rostro';
      });
      debugPrint('Error durante identificación facial: $error');
    } finally {
      if (shouldCooldown && session == _session) await _startCooldown();
    }
  }

  Future<void> _performMark(
    IdentifiedEmployee employee,
    String action,
    String successMessage,
  ) async {
    setState(() => _marking = true);
    try {
      await _attendanceApi.mark(employee.id, action, employee.recognitionToken);
      if (!mounted) return;
      setState(() {
        _pendingEmployee = null;
        _statusColor = const Color(0xFF52C41A);
        _message = '${employee.name}: $successMessage';
      });
    } finally {
      if (mounted) setState(() => _marking = false);
    }
  }

  Future<void> _chooseMark(String action, String successMessage) async {
    final employee = _pendingEmployee;
    if (employee == null || _marking) return;
    _selectionTimer?.cancel();
    final session = _session;
    XFile? confirmation;
    setState(() => _marking = true);
    try {
      final camera = _camera;
      if (camera == null || !_active) return;
      confirmation = await camera.takePicture();
      final current = await _attendanceApi.identify(confirmation.path);
      if (!mounted || !_active || session != _session) return;
      if (current.id != employee.id) {
        setState(() {
          _statusColor = Colors.orange;
          _message = 'La persona cambió. Identifícate nuevamente.';
        });
        return;
      }
      await _performMark(current, action, successMessage);
    } on DioException catch (error) {
      if (!mounted) return;
      if (error.response?.statusCode == 401) {
        await widget.onUnlink();
        return;
      }
      final data = error.response?.data;
      setState(() {
        _statusColor = Colors.redAccent;
        _message = data is Map
            ? data['message']?.toString() ?? 'No se pudo registrar la marcación'
            : 'No se pudo registrar la marcación';
      });
    } catch (_) {
      if (mounted) {
        setState(
          () => _message =
              'No se pudo confirmar la marcación. Identifícate nuevamente.',
        );
      }
    } finally {
      if (confirmation != null) {
        try {
          await File(confirmation.path).delete();
        } catch (_) {}
      }
      if (mounted) {
        setState(() {
          _marking = false;
          _pendingEmployee = null;
        });
      }
      if (session == _session) await _startCooldown();
    }
  }

  Future<void> _startCooldown() async {
    if (!mounted || !_active || _coolingDown) return;
    _scanTimer?.cancel();
    _selectionTimer?.cancel();
    _coolingDown = true;
    final session = _session;
    await Future<void>.delayed(const Duration(seconds: 3));
    if (!mounted || !_active || session != _session) {
      _coolingDown = false;
      return;
    }

    setState(() {
      _liveness = ActiveLivenessChallenge();
      _statusColor = const Color(0xFF1890FF);
      _awaitingDeparture = true;
      _emptyFrames = 0;
      _message = 'Retírate de la cámara para la siguiente marcación';
      _pendingEmployee = null;
      _coolingDown = false;
    });
    _scanTimer = Timer.periodic(
      const Duration(milliseconds: 500),
      (_) => _inspectFrame(),
    );
  }

  String _messageFor(FaceQualityIssue issue) => switch (issue) {
    FaceQualityIssue.noFace => 'Acércate a la cámara',
    FaceQualityIssue.multipleFaces => 'Debe aparecer una sola persona',
    FaceQualityIssue.tooSmall => 'Acércate un poco más',
    FaceQualityIssue.offCenter => 'Centra el rostro en el óvalo',
    FaceQualityIssue.headTurned => 'Mira de frente',
    FaceQualityIssue.headTilted => 'Mantén la cabeza recta',
    FaceQualityIssue.eyesClosed => 'Mantén los ojos abiertos',
  };

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.inactive ||
        state == AppLifecycleState.paused) {
      _scanTimer?.cancel();
      _active = false;
      _session++;
      _selectionTimer?.cancel();
      _pendingEmployee = null;
      _liveness.reset();
      _camera?.dispose();
      _camera = null;
    } else if (state == AppLifecycleState.resumed) {
      _active = true;
      _coolingDown = false;
      _initializeCamera();
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _scanTimer?.cancel();
    _selectionTimer?.cancel();
    _camera?.dispose();
    _faceDetection.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final camera = _camera;
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.kioskName),
        automaticallyImplyLeading: false,
        actions: [
          const _EcuadorClock(),
          IconButton(
            tooltip: 'Desvincular kiosco',
            icon: const Icon(Icons.link_off),
            onPressed: () async {
              final confirmed = await showDialog<bool>(
                context: context,
                builder: (dialogContext) => AlertDialog(
                  title: const Text('Desvincular kiosco'),
                  content: const Text(
                    'La tablet volverá a mostrar su identificador para generar credenciales nuevas.',
                  ),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(dialogContext, false),
                      child: const Text('Cancelar'),
                    ),
                    FilledButton(
                      onPressed: () => Navigator.pop(dialogContext, true),
                      child: const Text('Desvincular'),
                    ),
                  ],
                ),
              );
              if (confirmed == true) await widget.onUnlink();
            },
          ),
        ],
      ),
      body: camera == null || !camera.value.isInitialized
          ? Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(_message, textAlign: TextAlign.center),
                  const SizedBox(height: 16),
                  FilledButton.icon(
                    onPressed: _initializingCamera ? null : _initializeCamera,
                    icon: const Icon(Icons.refresh),
                    label: const Text('Reintentar cámara'),
                  ),
                ],
              ),
            )
          : Stack(
              fit: StackFit.expand,
              children: [
                CameraPreview(camera),
                Center(
                  child: Container(
                    width: 260,
                    height: 340,
                    decoration: BoxDecoration(
                      border: Border.all(color: _statusColor, width: 4),
                      borderRadius: const BorderRadius.all(
                        Radius.elliptical(260, 340),
                      ),
                    ),
                  ),
                ),
                Positioned(
                  left: 24,
                  right: 24,
                  bottom: 32,
                  child: Card(
                    color: Colors.black87,
                    child: Padding(
                      padding: const EdgeInsets.all(18),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            _message,
                            textAlign: TextAlign.center,
                            style: TextStyle(fontSize: 20, color: _statusColor),
                          ),
                          if (_pendingEmployee != null) ...[
                            const SizedBox(height: 16),
                            Row(
                              children: [
                                Expanded(
                                  child: FilledButton.tonal(
                                    onPressed: _marking || !_canStartBreak
                                        ? null
                                        : () => _chooseMark(
                                            'break/start',
                                            'Inicio de descanso registrado',
                                          ),
                                    child: const Text('Iniciar descanso'),
                                  ),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: FilledButton(
                                    onPressed: _marking
                                        ? null
                                        : () => _chooseMark(
                                            'clock-out',
                                            'Salida registrada',
                                          ),
                                    child: const Text('Registrar salida'),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
    );
  }
}

class _EcuadorClock extends StatefulWidget {
  const _EcuadorClock();

  @override
  State<_EcuadorClock> createState() => _EcuadorClockState();
}

class _EcuadorClockState extends State<_EcuadorClock> {
  late DateTime _time;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _time = _nowInEcuador();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() => _time = _nowInEcuador());
    });
  }

  static DateTime _nowInEcuador() =>
      DateTime.now().toUtc().subtract(const Duration(hours: 5));

  String get _formattedTime =>
      '${_time.hour.toString().padLeft(2, '0')}:'
      '${_time.minute.toString().padLeft(2, '0')}:'
      '${_time.second.toString().padLeft(2, '0')}';

  String get _formattedDate {
    const weekdays = [
      'lunes',
      'martes',
      'miércoles',
      'jueves',
      'viernes',
      'sábado',
      'domingo',
    ];
    const months = [
      'enero',
      'febrero',
      'marzo',
      'abril',
      'mayo',
      'junio',
      'julio',
      'agosto',
      'septiembre',
      'octubre',
      'noviembre',
      'diciembre',
    ];
    return '${weekdays[_time.weekday - 1]}, '
        '${_time.day} de ${months[_time.month - 1]}';
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 6),
    child: Column(
      mainAxisAlignment: MainAxisAlignment.center,
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Text(
          _formattedTime,
          style: const TextStyle(
            fontSize: 22,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.2,
            fontFeatures: [FontFeature.tabularFigures()],
          ),
        ),
        Text(_formattedDate, style: const TextStyle(fontSize: 11)),
      ],
    ),
  );
}
