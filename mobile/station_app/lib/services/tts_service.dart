import 'dart:collection';
import 'package:flutter/foundation.dart';
import 'package:flutter_tts/flutter_tts.dart';
import '../models/station_item.dart';

class TtsService {
  final FlutterTts _tts = FlutterTts();
  final Queue<String> _queue = Queue();
  bool _processing = false;
  int _generation = 0;
  bool enabled = true;
  bool _voiceBusy = false;
  String? _current;
  Future<void> _voiceChanges = Future.value();

  void setVoiceBusy(bool busy) {
    _voiceChanges = _voiceChanges
        .then((_) async {
          if (_voiceBusy == busy) return;
          _voiceBusy = busy;
          if (busy) {
            _generation++;
            if (_current != null) _queue.addFirst(_current!);
            _current = null;
            try {
              await _tts.stop();
            } finally {
              _processing = false;
            }
          } else if (enabled && !_processing && _queue.isNotEmpty) {
            _processQueue();
          }
        })
        .catchError((Object error) {
          debugPrint('[TTS] Voice coordination: $error');
        });
  }

  Future<void> init() async {
    // Intentar locale ecuatoriano; el motor de Google puede tener es-EC o caer en es-US.
    final langs = await _tts.getLanguages as List?;
    final available = langs?.map((e) => e.toString()).toList() ?? [];

    final preferred = ['es-EC', 'es_EC', 'es-US', 'es_US', 'es-ES', 'es'];
    String chosen = 'es';
    for (final lang in preferred) {
      if (available.any((l) => l.toLowerCase() == lang.toLowerCase())) {
        chosen = lang;
        break;
      }
    }
    await _tts.setLanguage(chosen);

    // Ritmo un poco mas lento para mayor claridad en ambiente ruidoso de cocina.
    await _tts.setSpeechRate(0.44);
    await _tts.setVolume(1.0);
    await _tts.setPitch(1.0);
    await _tts.awaitSpeakCompletion(true);
  }

  void enqueue(String text) {
    if (!enabled || text.trim().isEmpty) return;
    _queue.add(text);
    if (!_processing && !_voiceBusy) _processQueue();
  }

  Future<void> _processQueue() async {
    _processing = true;
    final generation = _generation;
    try {
      while (_queue.isNotEmpty &&
          enabled &&
          !_voiceBusy &&
          generation == _generation) {
        final text = _queue.removeFirst();
        _current = text;
        try {
          await _tts.speak(text);
          if (generation == _generation) _current = null;
        } catch (e) {
          debugPrint('[TTS] No se pudo reproducir el aviso: $e');
        } finally {
          if (generation == _generation) _current = null;
        }
      }
    } finally {
      if (generation == _generation) _processing = false;
    }
  }

  Future<void> stop() async {
    _generation++;
    _queue.clear();
    _current = null;
    try {
      await _tts.stop();
    } catch (e) {
      debugPrint('[TTS] No se pudo detener el motor de voz: $e');
    } finally {
      _processing = false;
    }
  }

  Future<void> dispose() async => stop();

  static String buildAnnouncement(List<StationItem> items) =>
      _build('Pedido nuevo', items);

  static String buildAdditionAnnouncement(List<StationItem> items) =>
      _build('Item adicional', items);

  static String buildItemNotesUpdated(StationItem item, String? notes) {
    final label = item.tableCode != null && item.tableCode!.isNotEmpty
        ? 'Mesa ${item.tableCode}'
        : 'numero ${item.orderNumber}';
    final cleanNotes = notes?.trim();
    if (cleanNotes == null || cleanNotes.isEmpty) {
      return 'Observacion eliminada. $label. ${item.itemName}.';
    }
    return 'Observacion actualizada. $label. ${item.itemName}. $cleanNotes.';
  }

  static String _build(String prefix, List<StationItem> items) {
    if (items.isEmpty) return '';
    final first = items.first;
    final sb = StringBuffer('$prefix. ');

    if (first.tableCode != null && first.tableCode!.isNotEmpty) {
      sb.write('Mesa ${first.tableCode}. ');
    } else if (first.customerName != null && first.customerName!.isNotEmpty) {
      sb.write('${first.customerName}. ');
    } else {
      sb.write('Numero ${first.orderNumber}. ');
    }

    final orderNotes = first.orderNotes?.trim();
    if (orderNotes != null && orderNotes.isNotEmpty) {
      sb.write('Observacion general: $orderNotes. ');
    }

    for (final item in items) {
      final qty = item.quantity == 1 ? 'un' : '${item.quantity}';
      sb.write('$qty ${item.itemName}');
      final modifierLabels = item.modifierSelections
          .map((c) => c.label)
          .where((label) => label.isNotEmpty)
          .toList();
      if (modifierLabels.isNotEmpty) {
        sb.write(', ${modifierLabels.join(', ')}');
      }
      if (item.notes != null && item.notes!.isNotEmpty) {
        sb.write(', ${item.notes}');
      }
      sb.write('. ');
    }

    return sb.toString();
  }
}
