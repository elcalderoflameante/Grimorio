import 'dart:collection';
import 'package:flutter_tts/flutter_tts.dart';
import '../models/station_item.dart';

class TtsService {
  final FlutterTts _tts = FlutterTts();
  final Queue<String> _queue = Queue();
  bool _processing = false;
  bool enabled = true;

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
    if (!enabled) return;
    _queue.add(text);
    if (!_processing) _processQueue();
  }

  Future<void> _processQueue() async {
    _processing = true;
    while (_queue.isNotEmpty) {
      final text = _queue.removeFirst();
      await _tts.speak(text);
    }
    _processing = false;
  }

  Future<void> stop() async {
    _queue.clear();
    await _tts.stop();
    _processing = false;
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
      if (item.ingredientChoices.isNotEmpty) {
        sb.write(
          ', ${item.ingredientChoices.map((c) => c.chosenArticleName).join(', ')}',
        );
      }
      if (item.notes != null && item.notes!.isNotEmpty) {
        sb.write(', ${item.notes}');
      }
      sb.write('. ');
    }

    return sb.toString();
  }
}
