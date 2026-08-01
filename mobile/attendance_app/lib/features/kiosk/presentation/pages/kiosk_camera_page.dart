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
  const KioskCameraPage({super.key, required this.kioskName});
  final String kioskName;

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
  bool _processing = false;
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
    try {
      final available = await availableCameras();
      final selected = available.firstWhere(
        (item) => item.lensDirection == CameraLensDirection.front,
        orElse: () => available.first,
      );
      final controller = CameraController(
        selected,
        ResolutionPreset.medium,
        enableAudio: false,
      );
      await controller.initialize();
      if (!mounted) {
        await controller.dispose();
        return;
      }
      setState(() => _camera = controller);
      _scanTimer = Timer.periodic(
        const Duration(milliseconds: 350),
        (_) => _inspectFrame(),
      );
    } catch (_) {
      if (mounted) {
        setState(() => _message = 'No se pudo iniciar la cámara frontal');
      }
    }
  }

  Future<void> _inspectFrame() async {
    final camera = _camera;
    if (_processing ||
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
      if (!mounted) return;
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
          _message = 'Error de detección: $error';
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
    var shouldCooldown = true;
    try {
      final employee = await _attendanceApi.identify(imagePath);
      if (!mounted) return;
      final status = await _attendanceApi.getToday(employee.id);
      if (!mounted) return;

      if (status.status == null) {
        await _performMark(employee, 'clock-in', 'Entrada registrada');
      } else if (status.status == 2) {
        await _performMark(employee, 'break/end', 'Fin de descanso registrado');
      } else if (status.status == 1 && status.breakStartedAtUtc != null) {
        await _performMark(employee, 'clock-out', 'Salida registrada');
      } else if (status.status == 1) {
        shouldCooldown = false;
        setState(() {
          _pendingEmployee = employee;
          _statusColor = const Color(0xFF1890FF);
          _message = '${employee.name}, selecciona la marcación';
        });
      } else {
        setState(() {
          _statusColor = Colors.orange;
          _message = '${employee.name}, tu jornada ya fue finalizada';
        });
      }
    } on DioException catch (error) {
      if (!mounted) return;
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
      if (shouldCooldown) await _startCooldown();
    }
  }

  Future<void> _performMark(
    IdentifiedEmployee employee,
    String action,
    String successMessage,
  ) async {
    setState(() => _marking = true);
    try {
      await _attendanceApi.mark(employee.id, action);
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
    try {
      await _performMark(employee, action, successMessage);
    } on DioException catch (error) {
      if (!mounted) return;
      final data = error.response?.data;
      setState(() {
        _statusColor = Colors.redAccent;
        _message = data is Map
            ? data['message']?.toString() ?? 'No se pudo registrar la marcación'
            : 'No se pudo registrar la marcación';
      });
    } finally {
      await _startCooldown();
    }
  }

  Future<void> _startCooldown() async {
    _coolingDown = true;
    await Future<void>.delayed(const Duration(seconds: 3));
    if (!mounted) return;

    setState(() {
      _liveness = ActiveLivenessChallenge();
      _statusColor = const Color(0xFF1890FF);
      _message = _liveness.instruction;
      _pendingEmployee = null;
      _coolingDown = false;
    });
    _scanTimer = Timer.periodic(
      const Duration(milliseconds: 350),
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
      _camera?.dispose();
      _camera = null;
    } else if (state == AppLifecycleState.resumed) {
      _initializeCamera();
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _scanTimer?.cancel();
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
      ),
      body: camera == null || !camera.value.isInitialized
          ? Center(child: Text(_message))
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
                                    onPressed: _marking
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
