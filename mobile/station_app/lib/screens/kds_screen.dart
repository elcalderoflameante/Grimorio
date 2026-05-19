import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../providers/station_provider.dart';
import '../widgets/order_card.dart';
import '../widgets/completed_sidebar.dart';

class KdsScreen extends StatelessWidget {
  const KdsScreen({super.key});

  Future<void> _openOfflineSpeech(BuildContext context) async {
    final opened = await context.read<StationProvider>().openSpeechOfflineSettings();
    if (!opened && context.mounted) {
      showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          backgroundColor: const Color(0xFF1C1C3A),
          title: const Text('Voz offline',
              style: TextStyle(color: Colors.white, fontSize: 16)),
          content: const Text(
            'Este dispositivo no tiene una pantalla directa para descargar '
            'idiomas de voz.\n\n'
            'Ve a:\nAjustes → Sistema → Idioma e introducción de texto → '
            'Entrada de voz → Reconocimiento de voz de Google → '
            'Idiomas descargados\n\n'
            'y descarga Español (Ecuador) o Español (EE.UU.).',
            style: TextStyle(color: Colors.white70, fontSize: 13),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Entendido',
                  style: TextStyle(color: Color(0xFFE94560))),
            ),
          ],
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<StationProvider>();

    return Scaffold(
      backgroundColor: const Color(0xFF0A0A14),
      appBar: AppBar(
        backgroundColor: const Color(0xFF12122A),
        titleSpacing: 16,
        toolbarHeight: 48,
        title: Row(
          children: [
            const Icon(Icons.restaurant_menu, color: Color(0xFFE94560), size: 20),
            const SizedBox(width: 10),
            Text(
              provider.stationName ?? 'Estación',
              style: const TextStyle(
                  color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(width: 16),
            _ConnectionBadge(state: provider.connectionState),
          ],
        ),
        actions: [
          // Contador pedidos activos
          if (provider.items.any((e) => e.status == 'Pending'))
            Center(
              child: Container(
                margin: const EdgeInsets.symmetric(horizontal: 6),
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: Colors.orange.withValues(alpha: 0.2),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: Colors.orange.withValues(alpha: 0.5)),
                ),
                child: Text(
                  '${provider.items.where((e) => e.status == 'Pending').length} pendientes',
                  style: const TextStyle(
                      color: Colors.orange, fontWeight: FontWeight.bold, fontSize: 12),
                ),
              ),
            ),
          // Indicador de micrófono activo
          if (provider.isListening)
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 4),
              child: _MicPulse(),
            ),
          // Toggle voz
          IconButton(
            tooltip: provider.ttsEnabled ? 'Silenciar voz' : 'Activar voz',
            icon: Icon(
              provider.ttsEnabled ? Icons.volume_up_rounded : Icons.volume_off_rounded,
              color: provider.ttsEnabled ? const Color(0xFF4EE87A) : Colors.white30,
              size: 22,
            ),
            onPressed: () =>
                context.read<StationProvider>().setTtsEnabled(!provider.ttsEnabled),
          ),
          PopupMenuButton<String>(
            icon: const Icon(Icons.more_vert, color: Colors.white54, size: 20),
            color: const Color(0xFF1C1C3A),
            onSelected: (value) {
              if (value == 'change') context.read<StationProvider>().changeStation();
              if (value == 'logout') context.read<StationProvider>().logout();
              if (value == 'offline_speech') _openOfflineSpeech(context);
            },
            itemBuilder: (_) => [
              const PopupMenuItem(
                value: 'change',
                child: Row(children: [
                  Icon(Icons.swap_horiz, color: Colors.white70, size: 18),
                  SizedBox(width: 8),
                  Text('Cambiar estación', style: TextStyle(color: Colors.white, fontSize: 14)),
                ]),
              ),
              const PopupMenuItem(
                value: 'offline_speech',
                child: Row(children: [
                  Icon(Icons.download_rounded, color: Colors.white70, size: 18),
                  SizedBox(width: 8),
                  Text('Descargar voz offline', style: TextStyle(color: Colors.white, fontSize: 14)),
                ]),
              ),
              const PopupMenuItem(
                value: 'logout',
                child: Row(children: [
                  Icon(Icons.logout, color: Colors.redAccent, size: 18),
                  SizedBox(width: 8),
                  Text('Cerrar sesión', style: TextStyle(color: Colors.redAccent, fontSize: 14)),
                ]),
              ),
            ],
          ),
          const SizedBox(width: 4),
        ],
      ),
      body: Column(
        children: [
          // Banner de error de conexión
          if (provider.errorMessage != null)
            _ErrorBanner(
              message: provider.errorMessage!,
              onRetry: () => context.read<StationProvider>().reconnect(),
              onDismiss: () => context.read<StationProvider>().clearError(),
            ),
          Expanded(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // ── Área principal ───────────────────────────────────────
                Expanded(child: _MainArea(provider: provider)),

                // ── Sidebar de completados ───────────────────────────────
                const CompletedSidebar(width: 220),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  final VoidCallback onDismiss;
  const _ErrorBanner({required this.message, required this.onRetry, required this.onDismiss});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      color: const Color(0xFF3D0000),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          const Icon(Icons.warning_amber_rounded, color: Colors.redAccent, size: 18),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              message,
              style: const TextStyle(color: Colors.white70, fontSize: 13),
              overflow: TextOverflow.ellipsis,
            ),
          ),
          TextButton(
            onPressed: onRetry,
            child: const Text('Reintentar', style: TextStyle(color: Colors.orangeAccent, fontSize: 13)),
          ),
          IconButton(
            icon: const Icon(Icons.close, color: Colors.white38, size: 16),
            onPressed: onDismiss,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(),
          ),
        ],
      ),
    );
  }
}

class _MainArea extends StatelessWidget {
  final StationProvider provider;
  const _MainArea({required this.provider});

  @override
  Widget build(BuildContext context) {
    if (provider.connectionState == HubConnectionState.Reconnecting) {
      return const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            CircularProgressIndicator(color: Color(0xFFE94560)),
            SizedBox(height: 14),
            Text('Reconectando...', style: TextStyle(color: Colors.white54, fontSize: 15)),
          ],
        ),
      );
    }

    final groups = provider.orderedGroups;

    if (groups.isEmpty) {
      return const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.check_circle_outline, color: Color(0xFF2ECC71), size: 64),
            SizedBox(height: 14),
            Text(
              'Todo listo',
              style: TextStyle(
                  color: Colors.white70, fontSize: 26, fontWeight: FontWeight.bold),
            ),
            SizedBox(height: 6),
            Text(
              'Los nuevos pedidos aparecerán aquí.',
              style: TextStyle(color: Colors.white38, fontSize: 14),
            ),
          ],
        ),
      );
    }

    // Wrap: los pedidos más antiguos quedan a la izquierda,
    // los nuevos se agregan a la derecha, y envuelven hacia abajo.
    return SingleChildScrollView(
      padding: const EdgeInsets.all(10),
      child: Wrap(
        spacing: 10,
        runSpacing: 10,
        alignment: WrapAlignment.start,
        crossAxisAlignment: WrapCrossAlignment.start,
        children: groups
            .map((entry) => OrderCard(
                  orderId: entry.key,
                  items: entry.value,
                ))
            .toList(),
      ),
    );
  }
}

class _ConnectionBadge extends StatelessWidget {
  final HubConnectionState state;
  const _ConnectionBadge({required this.state});

  @override
  Widget build(BuildContext context) {
    final (label, color) = switch (state) {
      HubConnectionState.Connected => ('Conectado', const Color(0xFF2ECC71)),
      HubConnectionState.Reconnecting => ('Reconectando...', Colors.orange),
      _ => ('Desconectado', Colors.redAccent),
    };

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 7,
          height: 7,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 5),
        Text(label, style: TextStyle(color: color, fontSize: 11)),
      ],
    );
  }
}

// Punto rojo pulsante que indica que el micrófono está escuchando
class _MicPulse extends StatefulWidget {
  const _MicPulse();

  @override
  State<_MicPulse> createState() => _MicPulseState();
}

class _MicPulseState extends State<_MicPulse>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;
  late final Animation<double> _scale;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    )..repeat(reverse: true);
    _scale = Tween<double>(begin: 0.7, end: 1.0).animate(
      CurvedAnimation(parent: _ctrl, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _scale,
      builder: (context, child) => Transform.scale(
        scale: _scale.value,
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 8,
              height: 8,
              decoration: BoxDecoration(
                color: Colors.redAccent.withValues(
                  alpha: 0.5 + 0.5 * math.sin(_ctrl.value * math.pi),
                ),
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: 4),
            const Icon(Icons.mic, color: Colors.redAccent, size: 16),
          ],
        ),
      ),
    );
  }
}
