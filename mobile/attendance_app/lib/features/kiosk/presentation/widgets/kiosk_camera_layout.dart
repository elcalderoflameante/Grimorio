import 'dart:math' as math;

import 'package:flutter/material.dart';

/// Keeps controls outside the full, uncropped camera image in either orientation.
class KioskCameraLayout extends StatelessWidget {
  const KioskCameraLayout({
    super.key,
    required this.preview,
    required this.previewAspectRatio,
    required this.message,
    required this.statusColor,
    this.showMarkActions = false,
    this.marking = false,
    this.canStartBreak = true,
    this.unconfirmedMark = false,
    this.recoveringMark = false,
    required this.onStartBreak,
    required this.onClockOut,
    required this.onRetry,
  });

  final Widget preview;
  final double previewAspectRatio;
  final String message;
  final Color statusColor;
  final bool showMarkActions;
  final bool marking;
  final bool canStartBreak;
  final bool unconfirmedMark;
  final bool recoveringMark;
  final VoidCallback onStartBreak;
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
          Semantics(
            liveRegion: true,
            child: Text(
              message,
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 20, color: statusColor),
            ),
          ),
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
              onPressed: marking || !canStartBreak ? null : onStartBreak,
              child: const Text(
                'Iniciar descanso',
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
}
