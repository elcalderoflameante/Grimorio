import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_background/flutter_background.dart';
import 'package:livekit_client/livekit_client.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'voice_controls.dart';

/// One authenticated voice session for the entire app, including pushed routes.
class VoiceShell extends StatefulWidget {
  const VoiceShell({
    super.key,
    required this.child,
    required this.apiBaseUrl,
    required this.readToken,
    this.onChannelBusy,
  });
  final Widget child;
  final String apiBaseUrl;
  final Future<String?> Function() readToken;
  final ValueChanged<bool>? onChannelBusy;
  @override
  State<VoiceShell> createState() => _VoiceShellState();
}

class _VoiceShellState extends State<VoiceShell> with WidgetsBindingObserver {
  HubConnection? _hub;
  Room? _room;
  EventsListener<RoomEvent>? _events;
  Timer? _retry, _heartbeat, _maximum;
  Timer? _buttonHeartbeat;
  final _controls = VoiceControls();
  int? _hardwareId;
  int _pressSequence = 0;
  Future<void>? _stopping;
  bool _cueBusy = false;
  bool _controlsOpen = false;

  void _notifyAudioBusy() => widget.onChannelBusy?.call(
    !_closing &&
        (_speaker != null ||
            _acquiring ||
            _speaking ||
            _stopping != null ||
            _cueBusy),
  );

  Future<void> _playCue(String kind) async {
    _cueBusy = true;
    _notifyAudioBusy();
    try {
      await _controls.tone(kind);
    } finally {
      _cueBusy = false;
      _notifyAudioBusy();
    }
  }

  bool _enabled = false, _connected = false, _speaking = false;
  bool _held = false, _acquiring = false, _renewing = false, _closing = false;
  bool _activating = false, _deactivating = false;
  Future<void> _cleanup = Future.value();
  int _generation = 0, _revision = -1, _count = 0;
  String _status = 'Walkie-talkie apagado', _identity = '';
  String? _lease, _speaker;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _controls.listen((down, id) {
      if (down) {
        if (_hardwareId == null && !_held && !_acquiring && _stopping == null) {
          unawaited(_talk(hardwareId: id));
        }
      } else if (_hardwareId == id) {
        unawaited(_stopTalking());
      }
    });
    unawaited(_controls.refresh());
  }

  void _update(VoidCallback update) {
    if (mounted && !_closing) setState(update);
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      unawaited(_controls.refresh());
    } else if (_hardwareId == null || state == AppLifecycleState.detached) {
      unawaited(_stopTalking());
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _closing = true;
    _enabled = false;
    _retry?.cancel();
    _buttonHeartbeat?.cancel();
    unawaited(_controls.arm(false));
    _controls.dispose();
    unawaited(_teardown(disableBackground: true));
    super.dispose();
  }

  Future<void> _activate() async {
    if (_enabled || _activating || _deactivating || _closing) return;
    _activating = true;
    _update(() {
      _enabled = true;
      _status = 'Preparando audio…';
    });
    try {
      if (!await Permission.microphone.request().isGranted) {
        throw StateError('Permite el micrófono para usar el walkie-talkie.');
      }
      if (_closing || !_enabled) return;
      if (Theme.of(context).platform == TargetPlatform.android) {
        await Permission.notification.request();
        final initialized = await FlutterBackground.initialize(
          androidConfig: const FlutterBackgroundAndroidConfig(
            notificationTitle: 'Walkie-talkie · Grimorio',
            notificationText: 'Canal general de la sucursal activo',
            notificationIcon: AndroidResource(
              name: 'ic_launcher',
              defType: 'mipmap',
            ),
          ),
        );
        if (!initialized ||
            !await FlutterBackground.enableBackgroundExecution()) {
          throw StateError('No se pudo activar la recepción en segundo plano.');
        }
      }
      if (_closing || !_enabled) {
        await _teardown(disableBackground: true);
        return;
      }
      await _connect();
    } catch (error) {
      await _deactivate();
      _update(
        () => _status = error is StateError
            ? error.message.toString()
            : 'No se pudo activar la voz. Revisa los permisos.',
      );
    } finally {
      _activating = false;
    }
  }

  Future<void> _connect({bool recovering = false}) async {
    final generation = ++_generation;
    _update(() {
      _status = 'Conectando voz…';
      _connected = false;
    });
    final base = widget.apiBaseUrl.replaceFirst(RegExp(r'/api/?$'), '');
    final hub = HubConnectionBuilder()
        .withUrl(
          '$base/hubs/voice',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => await widget.readToken() ?? '',
          ),
        )
        .build();
    _hub = hub;
    _revision = -1;
    hub.on('voice:state', (arguments) {
      if (generation != _generation || arguments == null || arguments.isEmpty)
        return;
      _applyState(Map<String, dynamic>.from(arguments.first as Map));
    });
    hub.onclose(({Exception? error}) {
      if (generation == _generation && _enabled) unawaited(_recover());
    });
    try {
      await hub.start();
      final result = Map<String, dynamic>.from(await hub.invoke('Join') as Map);
      if (generation != _generation || _closing) {
        await hub.stop();
        return;
      }
      _identity = result['identity'] as String;
      _applyState(Map<String, dynamic>.from(result['state'] as Map));
      final room = Room();
      _room = room;
      _events = room.createListener()
        ..on<RoomDisconnectedEvent>((event) {
          if (generation == _generation && _enabled) unawaited(_recover());
        })
        ..on<RoomReconnectingEvent>((event) {
          if (generation == _generation && _enabled) unawaited(_recover());
        })
        ..on<RoomResumingEvent>((event) {
          if (generation == _generation && _enabled) unawaited(_recover());
        });
      await room.connect(result['url'] as String, result['token'] as String);
      if (generation != _generation || _closing) {
        await room.disconnect();
        return;
      }
      await AudioManager.instance.setSpeakerOutputPreferred(true);
      _update(() {
        _connected = true;
        _status = 'Escuchando · canal general';
      });
      await _controls.arm(!_controlsOpen);
      if (generation != _generation || _closing || !_enabled) {
        await _controls.arm(false);
        return;
      }
      _buttonHeartbeat?.cancel();
      _buttonHeartbeat = Timer.periodic(const Duration(seconds: 2), (_) {
        unawaited(
          _controls.arm(_enabled && _connected && !_closing && !_controlsOpen),
        );
      });
    } catch (_) {
      if (generation != _generation) return;
      if (recovering && _enabled && !_closing) {
        await _recover();
        return;
      }
      await _deactivate();
      _update(
        () => _status =
            'No se pudo conectar. Revisa que el servicio de voz esté habilitado.',
      );
    }
  }

  void _applyState(Map<String, dynamic> state) {
    final revision = (state['revision'] as num).toInt();
    if (revision < _revision) return;
    _revision = revision;
    final speaker = state['speaker'] as Map?;
    _update(() {
      _speaker = speaker?['name'] as String?;
      _count = (state['participants'] as List).length;
    });
    _notifyAudioBusy();
    if (_lease != null &&
        (state['leaseId'] != _lease || speaker?['identity'] != _identity))
      unawaited(_stopTalking());
  }

  Future<void> _talk({int? hardwareId}) async {
    if (_controlsOpen ||
        !_enabled ||
        _closing ||
        !_connected ||
        _acquiring ||
        _lease != null ||
        _stopping != null ||
        _held)
      return;
    _hardwareId = hardwareId;
    _held = true;
    _acquiring = true;
    _notifyAudioBusy();
    final press = ++_pressSequence;
    final hub = _hub!, room = _room!, generation = _generation;
    Future<bool> stillHeld() async =>
        _held &&
        press == _pressSequence &&
        generation == _generation &&
        (hardwareId == null || await _controls.isHeld(hardwareId)) &&
        _held &&
        press == _pressSequence &&
        generation == _generation;
    try {
      if (!await stillHeld()) return;
      final lease = await hub.invoke('Acquire') as String?;
      if (lease == null) {
        if (!await stillHeld()) return;
        _update(() => _status = 'Canal ocupado. Espera tu turno.');
        await _playCue('busy');
        return;
      }
      if (!await stillHeld()) {
        await hub.invoke('Release', args: [lease]);
        return;
      }
      _lease = lease;
      for (
        var i = 0;
        i < 20 && room.localParticipant?.permissions.canPublish != true;
        i++
      ) {
        await Future<void>.delayed(const Duration(milliseconds: 100));
      }
      if (!await stillHeld() || _lease != lease) return;
      if (room.localParticipant?.permissions.canPublish != true)
        throw StateError('Micrófono no habilitado');
      // The cue finishes before opening capture so it is not sent to the channel.
      await _playCue('start');
      if (!await stillHeld() || _lease != lease) return;
      await room.localParticipant!.setMicrophoneEnabled(true);
      if (!await stillHeld() || _lease != lease) {
        await room.localParticipant!.setMicrophoneEnabled(false);
        return;
      }
      _update(() {
        _speaking = true;
        _status = 'Hablando · suelta para terminar';
      });
      _heartbeat = Timer.periodic(
        const Duration(milliseconds: 2500),
        (_) => unawaited(_renew()),
      );
      _maximum = Timer(
        const Duration(seconds: 28),
        () => unawaited(_stopTalking()),
      );
    } catch (_) {
      if (generation == _generation && press == _pressSequence) {
        await _stopTalking();
        await _playCue('busy');
        _update(
          () => _status =
              'No se pudo transmitir. Revisa el micrófono y la conexión.',
        );
      }
    } finally {
      _acquiring = false;
      if (press == _pressSequence && !_speaking) await _stopTalking();
      _notifyAudioBusy();
    }
  }

  Future<void> _renew() async {
    if (_renewing || _lease == null) return;
    final lease = _lease, generation = _generation;
    _renewing = true;
    try {
      final hardwareId = _hardwareId;
      final held = hardwareId == null || await _controls.isHeld(hardwareId);
      if (lease != _lease || generation != _generation) return;
      final renewed =
          held && await _hub?.invoke('Renew', args: [lease!]) == true;
      if (!renewed && lease == _lease && generation == _generation)
        await _stopTalking();
    } catch (_) {
      if (lease == _lease && generation == _generation) await _stopTalking();
    } finally {
      _renewing = false;
    }
  }

  Future<void> _stopTalking() {
    return _stopping ??= _finishTalking().whenComplete(() {
      _stopping = null;
      _notifyAudioBusy();
    });
  }

  Future<void> _finishTalking() async {
    ++_pressSequence;
    _held = false;
    _hardwareId = null;
    _heartbeat?.cancel();
    _maximum?.cancel();
    final lease = _lease;
    final room = _room, hub = _hub;
    final wasSpeaking = _speaking;
    _lease = null;
    _update(() => _speaking = false);
    await _controls.cancel();
    try {
      await room?.localParticipant?.setMicrophoneEnabled(false);
    } catch (_) {
      try {
        await room?.disconnect();
      } catch (_) {}
    }
    if (lease != null) {
      try {
        await hub?.invoke('Release', args: [lease]);
      } catch (_) {
        /* The server expires the lease and revokes publishing. */
      }
    }
    if (wasSpeaking && _connected && _enabled && !_closing)
      await _playCue('end');
    if (_connected && (wasSpeaking || lease != null))
      _update(() => _status = 'Escuchando · canal general');
  }

  Future<void> _teardown({bool disableBackground = false}) {
    ++_generation;
    _update(() => _connected = false);
    _cleanup = _cleanup.then((_) => _cleanupSession(disableBackground));
    return _cleanup;
  }

  Future<void> _cleanupSession(bool disableBackground) async {
    _buttonHeartbeat?.cancel();
    await _controls.arm(false);
    await _stopTalking();
    final room = _room, hub = _hub, events = _events;
    _room = null;
    _hub = null;
    _events = null;
    await events?.dispose();
    try {
      await room?.disconnect();
      await room?.dispose();
    } catch (_) {}
    try {
      await hub?.stop();
    } catch (_) {}
    if (disableBackground && FlutterBackground.isBackgroundExecutionEnabled) {
      try {
        await FlutterBackground.disableBackgroundExecution();
      } catch (_) {}
    }
    widget.onChannelBusy?.call(false);
    _update(() {
      _connected = false;
      _count = 0;
      _speaker = null;
    });
  }

  Future<void> _recover() async {
    if (!_enabled || _closing) return;
    await _teardown();
    if (!_enabled || _closing) return;
    _update(() => _status = 'Reconectando voz…');
    _retry?.cancel();
    _retry = Timer(const Duration(seconds: 5), () {
      if (_enabled && !_closing) unawaited(_connect(recovering: true));
    });
  }

  Future<void> _deactivate() async {
    if (_deactivating) return;
    _deactivating = true;
    _enabled = false;
    _retry?.cancel();
    try {
      await _teardown(disableBackground: true);
      _update(() => _status = 'Walkie-talkie apagado');
    } finally {
      _deactivating = false;
    }
  }

  Future<void> _showControls() async {
    if (_controlsOpen) return;
    _controlsOpen = true;
    await _controls.arm(false);
    await _stopTalking();
    await _controls.refresh();
    if (!mounted || _closing) {
      _controlsOpen = false;
      return;
    }
    var volume = _controls.volume, sounds = _controls.sounds;
    try {
      await showDialog<void>(
        context: context,
        builder: (dialogContext) => StatefulBuilder(
          builder: (context, setDialogState) => AlertDialog(
            title: const Text('Botón y sonidos del walkie'),
            content: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Hablar con volumen arriba'),
                    subtitle: const Text(
                      'Mantén para hablar y suelta para terminar. Con el canal activo, este botón deja de subir el volumen.',
                    ),
                    value: volume,
                    onChanged: (value) => setDialogState(() => volume = value),
                  ),
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Sonidos de walkie-talkie'),
                    subtitle: const Text(
                      'Inicio, fin y canal ocupado. Empieza a hablar cuando termine el tono.',
                    ),
                    value: sounds,
                    onChanged: (value) => setDialogState(() => sounds = value),
                  ),
                  const Text(
                    'Para usar el botón fuera de la app, habilita «Grimorio · botón del walkie-talkie» en Accesibilidad. Solo procesa el botón; no lee la pantalla. Con pantalla apagada depende del teléfono y requiere una prueba.',
                  ),
                  TextButton(
                    onPressed: () async {
                      await _controls.openAccessibility();
                    },
                    child: const Text('Abrir Accesibilidad'),
                  ),
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(dialogContext),
                child: const Text('Cancelar'),
              ),
              FilledButton(
                onPressed: () async {
                  await _controls.save(volume, sounds);
                  if (dialogContext.mounted) Navigator.pop(dialogContext);
                },
                child: const Text('Guardar'),
              ),
            ],
          ),
        ),
      );
    } finally {
      _controlsOpen = false;
      await _controls.arm(_connected && _enabled && !_closing);
    }
  }

  @override
  Widget build(BuildContext context) => Column(
    children: [
      Material(
        color: const Color(0xff18232d),
        child: SafeArea(
          bottom: false,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            child: Row(
              children: [
                IconButton(
                  tooltip: _enabled
                      ? 'Salir del canal'
                      : 'Activar walkie-talkie',
                  onPressed: () =>
                      unawaited(_enabled ? _deactivate() : _activate()),
                  icon: Icon(
                    _enabled ? Icons.volume_up : Icons.volume_off,
                    color: _connected ? Colors.greenAccent : Colors.white70,
                  ),
                ),
                Expanded(
                  child: Text(
                    _speaker == null
                        ? _status
                        : 'Habla $_speaker · $_count conectados',
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: Colors.white, fontSize: 12),
                  ),
                ),
                if (_controls.supported)
                  IconButton(
                    tooltip: 'Botón y sonidos del walkie',
                    onPressed: () => unawaited(_showControls()),
                    icon: const Icon(Icons.tune, color: Colors.white70),
                  ),
                if (_connected)
                  Semantics(
                    label: 'Mantener presionado para hablar',
                    button: true,
                    child: Listener(
                      onPointerDown: (_) => unawaited(_talk()),
                      onPointerUp: (_) => unawaited(_stopTalking()),
                      onPointerCancel: (_) => unawaited(_stopTalking()),
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 14,
                          vertical: 14,
                        ),
                        decoration: BoxDecoration(
                          color: _speaking
                              ? Colors.red.shade700
                              : Colors.teal.shade700,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          _speaking ? 'Hablando…' : 'Mantén para hablar',
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 12,
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
      Expanded(child: widget.child),
    ],
  );
}
