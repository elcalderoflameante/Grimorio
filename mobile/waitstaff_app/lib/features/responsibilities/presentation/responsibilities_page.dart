import 'dart:async';
import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';

import '../../../core/network/api_error.dart';
import '../../../core/widgets/app_update_gate.dart';
import '../data/responsibility_api_service.dart';
import '../data/responsibility_models.dart';

String _time(DateTime value) => DateFormat('HH:mm').format(value.toLocal());
String _stage(String value) => switch (value) {
  'Opening' => 'Inicio',
  'Closing' => 'Cierre',
  _ => 'Durante el turno',
};
String _status(String value) => switch (value) {
  'Overdue' => 'Vencida',
  'Completed' => 'Realizada',
  'CompletedWithIssue' => 'Con novedad',
  _ => 'Pendiente',
};

class ResponsibilitiesPage extends ConsumerStatefulWidget {
  const ResponsibilitiesPage({super.key});
  @override
  ConsumerState<ResponsibilitiesPage> createState() =>
      _ResponsibilitiesPageState();
}

class _ResponsibilitiesPageState extends ConsumerState<ResponsibilitiesPage>
    with WidgetsBindingObserver {
  ResponsibilityDay? _day;
  String? _error, _syncWarning;
  bool _loading = true, _refreshing = false;
  Timer? _timer;
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
    _timer = Timer.periodic(const Duration(seconds: 60), (_) {
      if (WidgetsBinding.instance.lifecycleState == AppLifecycleState.resumed) {
        unawaited(_load(silent: true));
      }
    });
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) unawaited(_load(silent: true));
  }

  @override
  void dispose() {
    _timer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  Future<void> _load({bool silent = false}) async {
    if (_refreshing || !mounted) return;
    _refreshing = true;
    if (!silent) setState(() => _loading = true);
    try {
      final api = ref.read(responsibilityApiProvider);
      String? warning;
      try {
        await api.recoverCamera();
        await api.flush();
      } catch (error) {
        warning = readableApiError(
          error,
          fallback:
              'Hay registros guardados en este teléfono pendientes de envío.',
        );
      }
      final day = await api.day();
      if (!mounted) return;
      ref.read(responsibilityPendingCountProvider.notifier).state = day
          .activities
          .where((a) => a.isPending)
          .length;
      setState(() {
        _day = day;
        _error = null;
        _syncWarning = warning;
      });
    } catch (error) {
      if (mounted) {
        setState(
          () => _error = readableApiError(
            error,
            fallback: 'No se pudo cargar tu jornada.',
          ),
        );
      }
    } finally {
      _refreshing = false;
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Mi jornada'),
      actions: [
        IconButton(
          tooltip: 'Sincronizar',
          onPressed: () => _load(),
          icon: const Icon(Icons.sync),
        ),
      ],
    ),
    body: _loading && _day == null
        ? const Center(child: CircularProgressIndicator())
        : RefreshIndicator(
            onRefresh: _load,
            child: ListView(
              padding: const EdgeInsets.all(16),
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                if (_day != null)
                  Text(
                    'Jornada ${_day!.workDate}',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                if (_day?.offline == true)
                  const _Notice(
                    'Sin conexión. Se muestra la última jornada guardada en este teléfono.',
                  ),
                if (_syncWarning != null) _Notice(_syncWarning!),
                if (_error != null) _Notice(_error!),
                if (_day?.message != null)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 20),
                    child: Text(_day!.message!),
                  ),
                ...?_day?.activities.map(
                  (activity) => Card(
                    child: ListTile(
                      contentPadding: const EdgeInsets.all(16),
                      leading: Icon(
                        activity.isPending
                            ? Icons.assignment_outlined
                            : Icons.task_alt,
                        color: activity.status == 'Overdue'
                            ? Colors.orange
                            : null,
                      ),
                      title: Text(
                        activity.taskName,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      subtitle: Text(
                        '${activity.areaName} · ${activity.positionName}\n${_stage(activity.stage)} · ${_time(activity.availableAt)}–${_time(activity.dueAt)}\n${_status(activity.status)}',
                      ),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () async {
                        final api = ref.read(responsibilityApiProvider);
                        await Navigator.of(context).push(
                          MaterialPageRoute<void>(
                            builder: (_) => ResponsibilityActivityPage(
                              initial: activity,
                              api: api,
                            ),
                          ),
                        );
                        await _load();
                      },
                    ),
                  ),
                ),
                const SizedBox(height: 24),
              ],
            ),
          ),
  );
}

class _Notice extends StatelessWidget {
  const _Notice(this.text);
  final String text;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 8),
    child: Text(text, style: const TextStyle(color: Colors.orangeAccent)),
  );
}

class ResponsibilityActivityPage extends StatefulWidget {
  const ResponsibilityActivityPage({
    super.key,
    required this.initial,
    required this.api,
  });
  final ResponsibilityActivity initial;
  final ResponsibilityApiService api;
  @override
  State<ResponsibilityActivityPage> createState() =>
      _ResponsibilityActivityPageState();
}

class _ResponsibilityActivityPageState
    extends State<ResponsibilityActivityPage> {
  late ResponsibilityActivity _activity;
  late final TextEditingController _notes;
  final Map<String, String> _answers = {};
  List<Map<String, dynamic>> _pendingPhotos = [];
  bool _hasIssue = false,
      _busy = false,
      _pendingCompletion = false,
      _dirty = false,
      _allowPop = false;
  String? _error;
  VoidCallback? _releaseUpdateDeferral;
  @override
  void initState() {
    super.initState();
    _activity = widget.initial;
    _notes = TextEditingController(text: _activity.notes ?? '');
    _answers.addAll(_activity.answers);
    _hasIssue = _activity.status == 'CompletedWithIssue';
    unawaited(_refresh());
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _releaseUpdateDeferral ??= AppUpdateGate.defer(context);
  }

  @override
  void dispose() {
    _releaseUpdateDeferral?.call();
    _notes.dispose();
    super.dispose();
  }

  bool get _editable => _activity.isAvailable && !_busy && !_pendingCompletion;
  bool get _answerHasIssue => _activity.checklist.any(
    (check) => check.attentionOptions.contains(_answers[check.key]),
  );
  Future<void> _refresh() async {
    try {
      final photos = await widget.api.pendingPhotos(_activity.id);
      final pending = await widget.api.hasPendingCompletion(_activity.id);
      if (!mounted) return;
      setState(() {
        _pendingPhotos = photos;
        _pendingCompletion = pending;
      });
      final current = await widget.api.activity(_activity.id);
      if (mounted) setState(() => _activity = current);
    } catch (_) {
      /* Preserve the current form and its local evidence when offline. */
    }
  }

  Future<void> _capture(String phase) async {
    if (!_editable) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    ResponsibilityCapture? ticket;
    bool saved = false;
    try {
      ticket = await widget.api.capture(_activity.id, phase);
      final clock = Stopwatch()..start();
      await widget.api.beginCameraSession(ticket, _activity.id, phase);
      final photo = await ImagePicker().pickImage(
        source: ImageSource.camera,
        preferredCameraDevice: CameraDevice.rear,
        maxWidth: 1600,
        maxHeight: 1600,
        imageQuality: 80,
      );
      if (photo == null) return;
      final capturedAt = ticket.issuedAt.add(clock.elapsed);
      if (capturedAt.isAfter(ticket.expiresAt)) {
        throw StateError(
          'La sesión de cámara caducó. Toma una fotografía nueva.',
        );
      }
      await widget.api.savePhoto(
        ticket,
        _activity.id,
        phase,
        File(photo.path),
        capturedAt,
      );
      saved = true;
      try {
        await widget.api.flush();
      } catch (error) {
        if (mounted) {
          setState(
            () => _error = readableApiError(
              error,
              fallback: 'Foto guardada en el teléfono. Pendiente de envío.',
            ),
          );
        }
      }
      await _refresh();
    } catch (error) {
      if (mounted) {
        setState(
          () => _error = readableApiError(
            error,
            fallback:
                'No se pudo tomar la foto. Conéctate para abrir una sesión de cámara.',
          ),
        );
      }
    } finally {
      await widget.api.finishCameraSession();
      if (ticket != null && !saved) await widget.api.cancelCapture(ticket.id);
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _save() async {
    if (_busy) return;
    if (!_pendingCompletion &&
        _activity.checklist.any((c) => !_answers.containsKey(c.key))) {
      setState(() => _error = 'Completa todos los controles.');
      return;
    }
    if (!_pendingCompletion &&
        (_hasIssue || _answerHasIssue) &&
        _notes.text.trim().isEmpty) {
      setState(() => _error = 'Describe la novedad encontrada.');
      return;
    }
    final phases = {
      ..._activity.evidence.map((e) => e.phase),
      ..._pendingPhotos.map((e) => e['phase'] as String),
    };
    if (!_pendingCompletion &&
        ((_activity.photoMode != 'None' && !phases.contains('After')) ||
            (_activity.photoMode == 'BeforeAndAfter' &&
                !phases.contains('Before')))) {
      setState(
        () => _error =
            'Toma las fotografías requeridas antes de registrar la actividad.',
      );
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      if (_pendingCompletion) {
        await widget.api.flush();
      } else {
        await widget.api.complete(
          _activity.id,
          Map.of(_answers),
          _hasIssue || _answerHasIssue,
          _notes.text.trim(),
        );
      }
      await _refresh();
      if (!mounted) return;
      if (_pendingCompletion || _activity.isPending) {
        throw StateError(
          'El registro sigue pendiente de confirmación del servidor.',
        );
      }
      setState(() {
        _dirty = false;
        _allowPop = true;
      });
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Actividad registrada.')));
    } catch (error) {
      await _refresh();
      if (mounted) {
        setState(
          () => _error = readableApiError(
            error,
            fallback:
                'Registro guardado en el teléfono. Reintenta para enviarlo; no se mostrará como realizado hasta que el servidor lo confirme.',
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _leave() async {
    if (_busy) return;
    final leave =
        !_dirty ||
        await showDialog<bool>(
              context: context,
              builder: (ctx) => AlertDialog(
                title: const Text('¿Salir de la actividad?'),
                content: const Text(
                  'Las respuestas sin registrar se perderán. Las fotos pendientes de envío permanecen guardadas.',
                ),
                actions: [
                  TextButton(
                    onPressed: () => Navigator.pop(ctx, false),
                    child: const Text('Continuar'),
                  ),
                  FilledButton(
                    onPressed: () => Navigator.pop(ctx, true),
                    child: const Text('Salir'),
                  ),
                ],
              ),
            ) ==
            true;
    if (!mounted || !leave) return;
    setState(() => _allowPop = true);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) Navigator.pop(context);
    });
  }

  Future<void> _viewPhoto(ResponsibilityEvidence evidence) async {
    try {
      final bytes = await widget.api.photo(evidence.id);
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (ctx) => Dialog(
          child: InteractiveViewer(
            child: Image.memory(Uint8List.fromList(bytes)),
          ),
        ),
      );
    } catch (error) {
      if (mounted) {
        setState(
          () => _error = readableApiError(
            error,
            fallback: 'No se pudo cargar la evidencia.',
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !_busy && (!_dirty || _allowPop),
    onPopInvokedWithResult: (didPop, _) {
      if (!didPop) unawaited(_leave());
    },
    child: Scaffold(
      appBar: AppBar(
        title: Text(_activity.taskName),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: _leave,
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            '${_activity.areaName} · ${_activity.positionName}',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          Text(
            '${_stage(_activity.stage)} · ${_time(_activity.availableAt)}–${_time(_activity.dueAt)} · ${_status(_activity.status)}',
          ),
          if (_activity.instructions.isNotEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 16),
              child: Text(_activity.instructions),
            ),
          if (_activity.isPending && !_activity.isAvailable)
            const _Notice('Esta actividad se habilitará a la hora indicada.'),
          if (_pendingCompletion)
            const _Notice(
              'Registro pendiente de confirmar. Reintenta el envío antes de modificar respuestas.',
            ),
          if (_error != null) _Notice(_error!),
          ..._activity.checklist.map(
            (check) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    check.label,
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 17,
                    ),
                  ),
                  Wrap(
                    spacing: 8,
                    runSpacing: 4,
                    children: check.options
                        .map(
                          (option) => ChoiceChip(
                            label: Text(option),
                            selected: _answers[check.key] == option,
                            onSelected: !_editable
                                ? null
                                : (_) => setState(() {
                                    _answers[check.key] = option;
                                    _dirty = true;
                                  }),
                          ),
                        )
                        .toList(),
                  ),
                ],
              ),
            ),
          ),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Hay una novedad o algo por resolver'),
            value: _hasIssue || _answerHasIssue,
            onChanged: !_editable || _answerHasIssue
                ? null
                : (value) => setState(() {
                    _hasIssue = value;
                    _dirty = true;
                  }),
          ),
          TextField(
            controller: _notes,
            enabled: _editable,
            maxLength: 2000,
            maxLines: 3,
            onChanged: (_) => setState(() => _dirty = true),
            decoration: const InputDecoration(
              labelText: 'Observaciones o detalle de la novedad',
              border: OutlineInputBorder(),
            ),
          ),
          if (_activity.photoMode != 'None') ...[
            const SizedBox(height: 16),
            const Text(
              'Evidencia fotográfica',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
            ),
            const Text(
              'Cámara al momento. Fotografía el área desocupada, evitando personas.',
            ),
            const SizedBox(height: 8),
            if (_activity.photoMode == 'BeforeAndAfter')
              FilledButton.icon(
                onPressed: _editable ? () => _capture('Before') : null,
                icon: const Icon(Icons.camera_alt),
                label: const Text('Tomar foto de antes'),
              ),
            const SizedBox(height: 8),
            FilledButton.icon(
              onPressed: _editable ? () => _capture('After') : null,
              icon: const Icon(Icons.camera_alt),
              label: const Text('Tomar foto al terminar'),
            ),
            ..._activity.evidence.map(
              (e) => TextButton.icon(
                onPressed: () => _viewPhoto(e),
                icon: const Icon(Icons.photo),
                label: Text(
                  '${e.phase == 'Before' ? 'Antes' : 'Después'} · enviada',
                ),
              ),
            ),
            ..._pendingPhotos.map(
              (e) => ListTile(
                title: Text(
                  '${e['phase'] == 'Before' ? 'Antes' : 'Después'} · pendiente de envío',
                ),
                trailing: IconButton(
                  tooltip: 'Descartar foto pendiente',
                  icon: const Icon(Icons.delete_outline),
                  onPressed: _busy || _pendingCompletion
                      ? null
                      : () async {
                          await widget.api.discardPhoto(
                            e['captureId'] as String,
                          );
                          await _refresh();
                        },
                ),
              ),
            ),
          ],
          const SizedBox(height: 24),
        ],
      ),
      bottomNavigationBar: !_activity.isPending
          ? null
          : SafeArea(
              minimum: const EdgeInsets.all(16),
              child: FilledButton.icon(
                onPressed:
                    _busy || (!_activity.isAvailable && !_pendingCompletion)
                    ? null
                    : _save,
                icon: _busy
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.check_circle),
                label: Text(
                  _pendingCompletion
                      ? 'Verificar / reintentar envío'
                      : _hasIssue || _answerHasIssue
                      ? 'Registrar con novedad'
                      : 'Registrar actividad',
                ),
              ),
            ),
    ),
  );
}
