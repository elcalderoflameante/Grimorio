import 'dart:async';

import 'package:flutter/material.dart';

class EcuadorClock extends StatefulWidget {
  const EcuadorClock({super.key});

  @override
  State<EcuadorClock> createState() => _EcuadorClockState();
}

class _EcuadorClockState extends State<EcuadorClock> {
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
  Widget build(BuildContext context) {
    final compact = MediaQuery.sizeOf(context).width < 600;
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: compact ? 8 : 20, vertical: 6),
      child: FittedBox(
        fit: BoxFit.scaleDown,
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              _formattedTime,
              style: TextStyle(
                fontSize: compact ? 18 : 22,
                fontWeight: FontWeight.w700,
                letterSpacing: 1.2,
                fontFeatures: const [FontFeature.tabularFigures()],
              ),
            ),
            Text(
              compact
                  ? '${_time.day.toString().padLeft(2, '0')}/'
                        '${_time.month.toString().padLeft(2, '0')}/${_time.year}'
                  : _formattedDate,
              style: const TextStyle(fontSize: 11),
            ),
          ],
        ),
      ),
    );
  }
}
