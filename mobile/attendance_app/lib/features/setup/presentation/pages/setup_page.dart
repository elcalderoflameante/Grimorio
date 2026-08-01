import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../../core/config/api_config.dart';
import '../../../../core/storage/kiosk_credentials_store.dart';
import '../../../kiosk/presentation/pages/kiosk_camera_page.dart';

class SetupPage extends StatefulWidget {
  const SetupPage({super.key});

  @override
  State<SetupPage> createState() => _SetupPageState();
}

class _SetupPageState extends State<SetupPage> {
  final _formKey = GlobalKey<FormState>();
  final _kioskIdController = TextEditingController();
  final _apiKeyController = TextEditingController();
  final _store = KioskCredentialsStore();
  String? _deviceIdentifier;
  String? _error;
  String? _linkedName;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _initialize();
  }

  Future<void> _initialize() async {
    final identifier = await _store.getOrCreateDeviceIdentifier();
    final existing = await _store.read();
    if (!mounted) return;
    setState(() => _deviceIdentifier = identifier);
    if (existing != null) {
      _kioskIdController.text = existing.kioskId;
      _apiKeyController.text = existing.apiKey;
      await _validateAndSave(showProgress: false);
    }
  }

  Future<void> _validateAndSave({bool showProgress = true}) async {
    if (showProgress && !_formKey.currentState!.validate()) return;
    setState(() {
      _saving = true;
      _error = null;
    });
    final credentials = KioskCredentials(
      kioskId: _kioskIdController.text.trim(),
      apiKey: _apiKeyController.text.trim(),
    );
    try {
      final response = await Dio(BaseOptions(baseUrl: ApiConfig.baseUrl))
          .get<Map<String, dynamic>>(
            '/attendance/kiosk/ping',
            options: Options(
              headers: {
                'X-Grimorio-Kiosk-Id': credentials.kioskId,
                'X-Grimorio-Kiosk-Key': credentials.apiKey,
              },
            ),
          );
      await _store.save(credentials);
      if (mounted) {
        setState(
          () => _linkedName = response.data?['name']?.toString() ?? 'Kiosco',
        );
      }
    } on DioException catch (error) {
      if (mounted) {
        setState(
          () => _error = error.response?.statusCode == 401
              ? 'Las credenciales no son válidas o el kiosco fue revocado.'
              : 'No se pudo conectar con el servidor Grimorio.',
        );
      }
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  @override
  void dispose() {
    _kioskIdController.dispose();
    _apiKeyController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_linkedName != null) {
      return KioskCameraPage(kioskName: _linkedName!);
    }
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 560),
              child: Form(
                key: _formKey,
                child: Column(
                  children: [
                    const Icon(
                      Icons.shield_outlined,
                      size: 88,
                      color: Color(0xFF1890FF),
                    ),
                    const SizedBox(height: 20),
                    const Text(
                      'Grimorio Asistencia',
                      style: TextStyle(
                        fontSize: 32,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 28),
                    ...[
                      const Text(
                        '1. Registra este identificador desde RR. HH. → Kioscos de asistencia.',
                      ),
                      const SizedBox(height: 10),
                      Card(
                        child: ListTile(
                          title: SelectableText(
                            _deviceIdentifier ?? 'Generando identificador...',
                          ),
                          trailing: IconButton(
                            icon: const Icon(Icons.copy),
                            onPressed: _deviceIdentifier == null
                                ? null
                                : () {
                                    Clipboard.setData(
                                      ClipboardData(text: _deviceIdentifier!),
                                    );
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      const SnackBar(
                                        content: Text('Identificador copiado'),
                                      ),
                                    );
                                  },
                          ),
                        ),
                      ),
                      const SizedBox(height: 20),
                      const Text(
                        '2. Ingresa las credenciales generadas por el ERP.',
                      ),
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: _kioskIdController,
                        decoration: const InputDecoration(
                          labelText: 'Kiosk ID',
                          border: OutlineInputBorder(),
                        ),
                        validator: (value) =>
                            value == null || value.trim().isEmpty
                            ? 'Campo obligatorio'
                            : null,
                      ),
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: _apiKeyController,
                        obscureText: true,
                        decoration: const InputDecoration(
                          labelText: 'Clave del kiosco',
                          border: OutlineInputBorder(),
                        ),
                        validator: (value) =>
                            value == null || value.trim().isEmpty
                            ? 'Campo obligatorio'
                            : null,
                      ),
                      if (_error != null) ...[
                        const SizedBox(height: 12),
                        Text(
                          _error!,
                          style: const TextStyle(color: Colors.redAccent),
                        ),
                      ],
                      const SizedBox(height: 20),
                      SizedBox(
                        width: double.infinity,
                        height: 52,
                        child: FilledButton(
                          onPressed: _saving ? null : _validateAndSave,
                          child: _saving
                              ? const CircularProgressIndicator()
                              : const Text('VALIDAR Y VINCULAR'),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
