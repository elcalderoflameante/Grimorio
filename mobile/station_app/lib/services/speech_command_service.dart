import 'dart:async';
import 'package:flutter/services.dart';
import '../models/station_item.dart';

class ParsedCommand {
  final String orderLabel;
  final List<StationItem> items;
  final String newStatus;

  ParsedCommand({
    required this.orderLabel,
    required this.items,
    required this.newStatus,
  });
}

enum _SpeechIgnoreReason { noKeyword, noOrder }

const _speechMethod = MethodChannel('com.grimorio.station_app/speech');
const _speechEvents = EventChannel('com.grimorio.station_app/speech_events');
const _stopWords = {
  'el',
  'la',
  'los',
  'las',
  'un',
  'una',
  'unos',
  'unas',
  'de',
  'del',
  'en',
  'para',
  'por',
  'favor',
  'favorcito',
  'porfa',
  'porfavor',
  'item',
  'plato',
  'producto',
};

class SpeechCommandService {
  bool _active = false;
  bool _micPaused = false;
  bool enabled = true;
  bool available = false;
  bool permissionGranted = false;
  Timer? _resumeTimer;

  List<StationItem> _activeItems = [];
  StreamSubscription<dynamic>? _sub;

  Function(ParsedCommand command)? onCommand;
  Function(String reason)? onCommandNotUnderstood;
  Function(bool isListening)? onListeningChanged;
  Function(String message)? onUnavailable;

  Future<bool> init() async {
    try {
      available = await _speechMethod.invokeMethod<bool>('isAvailable') ?? false;
      permissionGranted =
          await _speechMethod.invokeMethod<bool>('hasAudioPermission') ?? false;
      return available;
    } catch (_) {
      available = false;
      permissionGranted = false;
      return false;
    }
  }

  void updateItems(List<StationItem> items) => _activeItems = items;

  Future<void> startListening() async {
    if (_active || !enabled) return;
    if (!available) {
      onUnavailable?.call('Reconocimiento de voz no disponible en este dispositivo.');
      return;
    }

    try {
      permissionGranted =
          await _speechMethod.invokeMethod<bool>('requestAudioPermission') ?? false;
    } on PlatformException catch (e) {
      onUnavailable?.call(e.message ?? 'No se pudo solicitar el permiso de micrófono.');
      return;
    }

    if (!permissionGranted) {
      onUnavailable?.call('Permiso de micrófono no concedido.');
      return;
    }

    _active = true;
    _sub = _speechEvents.receiveBroadcastStream().listen(
      (event) {
        if (event is String) _onText(event);
      },
      onError: (_) {},
    );

    try {
      await _speechMethod.invokeMethod('startListening');
      onListeningChanged?.call(true);
    } on PlatformException catch (e) {
      await _sub?.cancel();
      _sub = null;
      _active = false;
      onListeningChanged?.call(false);
      onUnavailable?.call(e.message ?? 'No se pudo iniciar el micrófono.');
    }
  }

  void _onText(String text) {
    if (!_active || !enabled || _micPaused) return;
    final clean = text.trim();
    if (clean.isEmpty) return;

    if (_isOwnSpeechResult(clean)) return;

    final outcome = _parse(clean);

    if (outcome is ParsedCommand) {
      onCommand?.call(outcome);
    } else if (outcome == _SpeechIgnoreReason.noOrder) {
      onCommandNotUnderstood?.call('No encontré esa mesa o pedido.');
    }
  }

  void pauseForTts() {
    _resumeTimer?.cancel();
    _micPaused = true;
  }

  void resumeFromTts() {
    _resumeTimer?.cancel();
    _resumeTimer = Timer(const Duration(milliseconds: 1400), () {
      _micPaused = false;
    });
  }

  Future<void> stop() async {
    _active = false;
    _micPaused = false;
    _resumeTimer?.cancel();
    _resumeTimer = null;
    await _sub?.cancel();
    _sub = null;
    try {
      await _speechMethod.invokeMethod('stopListening');
    } catch (_) {}
    onListeningChanged?.call(false);
  }

  Future<void> dispose() async => stop();

  bool _isOwnSpeechResult(String rawText) {
    final text = _normalizeText(rawText);
    return text.startsWith('oido chef') ||
        text.startsWith('no encontre') ||
        text.startsWith('reconocimiento de voz') ||
        text.startsWith('permiso de microfono') ||
        text.contains('oido chef');
  }

  Future<bool> openOfflineSettings() async {
    try {
      await _speechMethod.invokeMethod('openOfflineSettings');
      return true;
    } catch (_) {
      return false;
    }
  }

  Object _parse(String rawText) {
    final text = _normalizeText(_normalizeNumbers(rawText));

    String? newStatus;
    if (_containsAny(text, ['preparando', 'preparacion', 'proceso', 'haciendo'])) {
      newStatus = 'InPreparation';
    } else if (_containsAny(
      text,
      ['listo', 'lista', 'completo', 'completado', 'terminado', 'terminada'],
    )) {
      newStatus = 'Ready';
    }
    if (newStatus == null) return _SpeechIgnoreReason.noKeyword;

    final tableMatch = RegExp(
      r'\b(?:de|del|en|para|por)?\s*(?:la|el)?\s*mesas?\s+([a-z0-9]+)\b',
    ).firstMatch(text);
    final orderMatch = RegExp(
      r'\b(?:de|del|en|para|por)?\s*(?:el|la)?\s*(?:pedido|orden|comanda)\s+(\d+)\b',
    ).firstMatch(text);
    final tableCode = tableMatch?.group(1);
    final orderNumber = int.tryParse(orderMatch?.group(1) ?? '');

    if (tableCode == null && orderNumber == null) {
      return _SpeechIgnoreReason.noOrder;
    }

    final candidateItems = _activeItems.where((item) {
      final itemTable = _normalizeText(item.tableCode ?? '');
      final tableMatches = tableCode != null && itemTable == tableCode;
      final orderMatches = orderNumber != null && item.orderNumber == orderNumber;
      return (tableMatches || orderMatches) && _canAdvanceTo(item.status, newStatus!);
    }).toList();

    if (candidateItems.isEmpty) return _SpeechIgnoreReason.noOrder;

    final itemText = _extractItemText(text);
    if (itemText.isEmpty || _containsAny(itemText, ['todo', 'todos', 'all'])) {
      return ParsedCommand(
        orderLabel: candidateItems.first.orderLabel,
        items: candidateItems,
        newStatus: newStatus,
      );
    }

    final matched = _matchItem(candidateItems, itemText);
    if (matched == null) return _SpeechIgnoreReason.noOrder;

    return ParsedCommand(
      orderLabel: matched.orderLabel,
      items: [matched],
      newStatus: newStatus,
    );
  }

  bool _canAdvanceTo(String current, String target) {
    if (target == 'InPreparation') return current == 'Pending';
    if (target == 'Ready') return current == 'Pending' || current == 'InPreparation';
    return false;
  }

  String _extractItemText(String text) {
    return text
        .replaceAll(
          RegExp(r'\b(?:de|del|en|para|por)?\s*(?:la|el)?\s*mesas?\s+[a-z0-9]+\b'),
          '',
        )
        .replaceAll(
          RegExp(
            r'\b(?:de|del|en|para|por)?\s*(?:el|la)?\s*(?:pedido|orden|comanda)\s+\d+\b',
          ),
          '',
        )
        .replaceAll(RegExp(r'\b(?:preparando|preparacion|proceso|hacer|haciendo)\b'), '')
        .replaceAll(
          RegExp(r'\b(?:listo|lista|completo|completado|terminado|terminada|sale|sacar)\b'),
          '',
        )
        .replaceAll(RegExp('\\b(?:${_stopWords.join('|')})\\b'), '')
        .replaceAll(RegExp(r'\s+'), ' ')
        .trim();
  }

  StationItem? _matchItem(List<StationItem> candidates, String spokenText) {
    if (spokenText.isEmpty) return null;
    final spokenTokens = _tokens(spokenText);
    if (spokenTokens.isEmpty) return null;

    StationItem? best;
    var bestScore = 0.0;
    var secondBestScore = 0.0;

    for (final item in candidates) {
      final name = _normalizeText(_normalizeNumbers(item.itemName));
      final nameTokens = _tokens(name);
      var score = 0.0;

      if (name == spokenText) score += 8;
      if (name.contains(spokenText) || spokenText.contains(name)) score += 5;

      for (final token in spokenTokens) {
        if (nameTokens.contains(token)) {
          score += 2;
        } else if (name.contains(token)) {
          score += 1;
        }
      }

      if (nameTokens.isNotEmpty) {
        final matched = spokenTokens.where(nameTokens.contains).length;
        score += matched / nameTokens.length;
      }

      if (score > bestScore) {
        secondBestScore = bestScore;
        bestScore = score;
        best = item;
      } else if (score > secondBestScore) {
        secondBestScore = score;
      }
    }

    if (bestScore < 1.5) return null;
    if (secondBestScore > 0 && (bestScore - secondBestScore) < 0.75) return null;
    return best;
  }

  List<String> _tokens(String text) {
    return _normalizeText(text)
        .split(RegExp(r'\s+'))
        .where((word) => word.length > 1 && !_stopWords.contains(word))
        .toList();
  }

  bool _containsAny(String text, List<String> keywords) =>
      keywords.any((keyword) => text.contains(keyword));

  String _normalizeNumbers(String text) {
    const map = {
      'cero': '0',
      'uno': '1',
      'dos': '2',
      'tres': '3',
      'cuatro': '4',
      'cinco': '5',
      'seis': '6',
      'siete': '7',
      'ocho': '8',
      'nueve': '9',
      'diez': '10',
      'once': '11',
      'doce': '12',
      'trece': '13',
      'catorce': '14',
      'quince': '15',
      'dieciseis': '16',
      'dieciséis': '16',
      'diecisiete': '17',
      'dieciocho': '18',
      'diecinueve': '19',
      'veinte': '20',
      'treinta': '30',
    };

    var result = text.toLowerCase();
    map.forEach((word, digit) {
      result = result.replaceAll(RegExp('\\b$word\\b'), digit);
    });
    result = result
        .replaceAll(RegExp(r'\bveinti\s*uno\b'), '21')
        .replaceAll(RegExp(r'\bveinti\s*dos\b'), '22')
        .replaceAll(RegExp(r'\bveinti\s*tres\b'), '23')
        .replaceAll(RegExp(r'\bveinti\s*cuatro\b'), '24')
        .replaceAll(RegExp(r'\bveinti\s*cinco\b'), '25')
        .replaceAll(RegExp(r'\bveinti\s*seis\b'), '26')
        .replaceAll(RegExp(r'\bveinti\s*siete\b'), '27')
        .replaceAll(RegExp(r'\bveinti\s*ocho\b'), '28')
        .replaceAll(RegExp(r'\bveinti\s*nueve\b'), '29');
    return result;
  }

  String _normalizeText(String value) {
    return value
        .toLowerCase()
        .replaceAll('á', 'a')
        .replaceAll('é', 'e')
        .replaceAll('í', 'i')
        .replaceAll('ó', 'o')
        .replaceAll('ú', 'u')
        .replaceAll('ü', 'u')
        .replaceAll('ñ', 'n')
        .replaceAll(RegExp(r'[^a-z0-9\s]'), ' ')
        .replaceAll(RegExp(r'\s+'), ' ')
        .trim();
  }
}
