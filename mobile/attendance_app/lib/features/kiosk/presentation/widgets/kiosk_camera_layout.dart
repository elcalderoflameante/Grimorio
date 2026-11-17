import 'dart:math' as math;

import 'package:flutter/material.dart';

enum KioskFeedbackType { neutral, processing, success, warning, error }

class KioskDaySummary {
  const KioskDaySummary({
    required this.employeeName,
    required this.statusLabel,
    required this.clockIn,
    required this.breakPeriod,
    required this.clockOut,
    this.breakUsage,
  });

  final String employeeName;
  final String statusLabel;
  final String clockIn;
  final String breakPeriod;
  final String clockOut;
  final String? breakUsage;
}

/// Keeps controls outside the full, uncropped camera image in either orientation.
class KioskCameraLayout extends StatelessWidget {
  const KioskCameraLayout({
    super.key,
    required this.preview,
    required this.previewAspectRatio,
    required this.message,
    required this.statusColor,
    this.feedbackType = KioskFeedbackType.neutral,
    this.daySummary,
    this.showMarkActions = false,
    this.marking = false,
    this.canStartBreak = true,
    this.onBreak = false,
    this.unconfirmedMark = false,
    this.recoveringMark = false,
    required this.onStartBreak,
    required this.onEndBreak,
    required this.onClockOut,
    required this.onRetry,
  });

  final Widget preview;
  final double previewAspectRatio;
  final String message;
  final Color statusColor;
  final KioskFeedbackType feedbackType;
  final KioskDaySummary? daySummary;
  final bool showMarkActions;
  final bool marking;
  final bool canStartBreak;
  final bool onBreak;
  final bool unconfirmedMark;
  final bool recoveringMark;
  final VoidCallback onStartBreak;
  final VoidCallback onEndBreak;
  final VoidCallback onClockOut;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => SafeArea(
    top: false,
    child: LayoutBuilder(
      builder: (context, constraints) {
        final sideBySide =
            MediaQuery.orientationOf(context) == Orientation.landscape &&
            constraints.maxWidth >= 480;
        final padding = constraints.maxHeight < 400 ? 12.0 : 20.0;
        final camera = _buildPreview();
        final panel = _buildPanel();
        return Padding(
          padding: EdgeInsets.all(padding),
          child: sideBySide
              ? Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Expanded(child: camera),
                    const SizedBox(width: 16),
                    SizedBox(
                      width: (constraints.maxWidth * 0.40).clamp(200.0, 360.0),
                      child: panel,
                    ),
                  ],
                )
              : Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Expanded(child: camera),
                    const SizedBox(height: 12),
                    ConstrainedBox(
                      constraints: BoxConstraints(
                        maxHeight: (constraints.maxHeight - padding * 2) * 0.46,
                      ),
                      child: panel,
                    ),
                  ],
                ),
        );
      },
    ),
  );

  Widget _buildPreview() => ClipRRect(
    borderRadius: BorderRadius.circular(20),
    child: ColoredBox(
      color: Colors.black,
      child: Center(
        child: AspectRatio(
          key: const ValueKey('kiosk-camera-frame'),
          aspectRatio: previewAspectRatio,
          child: Stack(
            fit: StackFit.expand,
            children: [
              preview,
              IgnorePointer(
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final width = math.min(
                      constraints.maxWidth * 0.55,
                      constraints.maxHeight * 0.82 / 1.3,
                    );
                    return Align(
                      // Keep the guide slightly above the image center.
                      alignment: const Alignment(0, -0.04),
                      child: Container(
                        key: const ValueKey('kiosk-face-guide'),
                        width: width,
                        height: width * 1.3,
                        decoration: BoxDecoration(
                          border: Border.all(color: statusColor, width: 3),
                          borderRadius: BorderRadius.all(
                            Radius.elliptical(width, width * 1.3),
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    ),
  );

  Widget _buildPanel() => Card(
    key: const ValueKey('kiosk-status-panel'),
    margin: EdgeInsets.zero,
    child: SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AnimatedSwitcher(
            duration: const Duration(milliseconds: 220),
            transitionBuilder: (child, animation) => ScaleTransition(
              scale: animation,
              child: FadeTransition(opacity: animation, child: child),
            ),
            child: _buildFeedbackIndicator(),
          ),
          if (feedbackType != KioskFeedbackType.neutral)
            const SizedBox(height: 10),
          Semantics(
            liveRegion: true,
            child: Text(
              message,
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 20, color: statusColor),
            ),
          ),
          if (daySummary case final summary?) ...[
            const SizedBox(height: 14),
            _buildDaySummary(summary),
          ],
          if (unconfirmedMark || recoveringMark) ...[
            const SizedBox(height: 12),
            FilledButton.icon(
              style: FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(52),
              ),
              onPressed: recoveringMark ? null : onRetry,
              icon: const Icon(Icons.sync),
              label: Text(
                recoveringMark
                    ? 'Verificando marcación...'
                    : 'Reintentar conexión',
                textAlign: TextAlign.center,
              ),
            ),
          ],
          if (showMarkActions) ...[
            const SizedBox(height: 16),
            FilledButton.tonal(
              style: FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(52),
              ),
              onPressed: marking
                  ? null
                  : onBreak
                  ? onEndBreak
                  : canStartBreak
                  ? onStartBreak
                  : null,
              child: Text(
                onBreak ? 'Finalizar descanso' : 'Iniciar descanso',
                textAlign: TextAlign.center,
              ),
            ),
            const SizedBox(height: 12),
            FilledButton(
              style: FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(52),
              ),
              onPressed: marking ? null : onClockOut,
              child: const Text(
                'Registrar salida',
                textAlign: TextAlign.center,
              ),
            ),
          ],
        ],
      ),
    ),
  );

  Widget _buildDaySummary(KioskDaySummary summary) => DecoratedBox(
    key: const ValueKey('kiosk-day-summary'),
    decoration: BoxDecoration(
      color: statusColor.withValues(alpha: 0.08),
      borderRadius: BorderRadius.circular(14),
      border: Border.all(color: statusColor.withValues(alpha: 0.30)),
    ),
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  summary.employeeName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 5,
                ),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.16),
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  summary.statusLabel,
                  style: TextStyle(
                    color: statusColor,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          _summaryRow(Icons.login_rounded, 'Entrada', summary.clockIn),
          const SizedBox(height: 8),
          _summaryRow(
            Icons.coffee_outlined,
            'Descanso',
            summary.breakPeriod,
            detail: summary.breakUsage,
          ),
          const SizedBox(height: 8),
          _summaryRow(Icons.logout_rounded, 'Salida', summary.clockOut),
        ],
      ),
    ),
  );

  Widget _summaryRow(
    IconData icon,
    String label,
    String value, {
    String? detail,
  }) => Row(
    children: [
      Icon(icon, size: 20, color: statusColor),
      const SizedBox(width: 9),
      SizedBox(
        width: 72,
        child: Text(label, style: const TextStyle(color: Colors.white70)),
      ),
      Expanded(
        child: Text(
          detail == null ? value : '$value  ·  $detail',
          textAlign: TextAlign.end,
          style: const TextStyle(fontWeight: FontWeight.w600),
        ),
      ),
    ],
  );

  Widget _buildFeedbackIndicator() {
    if (feedbackType == KioskFeedbackType.neutral) {
      return const SizedBox.shrink(key: ValueKey('feedback-neutral'));
    }
    if (feedbackType == KioskFeedbackType.processing) {
      return const Center(
        key: ValueKey('feedback-processing'),
        child: SizedBox.square(
          dimension: 58,
          child: CircularProgressIndicator(strokeWidth: 6),
        ),
      );
    }

    final (icon, label) = switch (feedbackType) {
      KioskFeedbackType.success => (
        Icons.check_rounded,
        'Marcación confirmada',
      ),
      KioskFeedbackType.warning => (
        Icons.priority_high_rounded,
        'Marcación requiere atención',
      ),
      KioskFeedbackType.error => (
        Icons.close_rounded,
        'No se pudo registrar la marcación',
      ),
      _ => (Icons.info_outline, 'Estado de la marcación'),
    };
    return Semantics(
      key: ValueKey('feedback-${feedbackType.name}'),
      label: label,
      child: Center(
        child: Container(
          width: 76,
          height: 76,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: statusColor.withValues(alpha: 0.16),
            border: Border.all(color: statusColor, width: 4),
          ),
          child: Icon(icon, size: 54, color: statusColor),
        ),
      ),
    );
  }
}
