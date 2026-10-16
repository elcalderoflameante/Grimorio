import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../providers/station_provider.dart';
import '../widgets/order_card.dart';
import '../widgets/completed_sidebar.dart';

class KdsScreen extends StatefulWidget {
  const KdsScreen({super.key});

  @override
  State<KdsScreen> createState() => _KdsScreenState();
}

class _KdsScreenState extends State<KdsScreen> {
  late final Timer _clock;

  @override
  void initState() {
    super.initState();
    _clock = Timer.periodic(const Duration(minutes: 1), (_) => setState(() {}));
  }

  @override
  void dispose() {
    _clock.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<StationProvider>();

    return Scaffold(
      backgroundColor: const Color(0xFF0A0A14),
      appBar: AppBar(
        backgroundColor: const Color(0xFF12122A),
        titleSpacing: 8,
        toolbarHeight: 32,
        title: Row(
          children: [
            const Icon(
              Icons.restaurant_menu,
              color: Color(0xFFE94560),
              size: 13,
            ),
            const SizedBox(width: 5),
            Flexible(
              child: Text(
                provider.stationName ?? 'Estación',
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            const SizedBox(width: 8),
            _ConnectionBadge(state: provider.connectionState),
          ],
        ),
        actions: [
          if (provider.connectionState == HubConnectionState.Disconnected)
            IconButton(
              tooltip: 'Reconectar',
              icon: const Icon(Icons.refresh, size: 15),
              padding: EdgeInsets.zero,
              constraints: const BoxConstraints.tightFor(width: 30, height: 30),
              onPressed: provider.reconnect,
            ),
          // Contador pedidos activos
          if (provider.items.any((e) => e.status == 'Pending'))
            Center(
              child: Container(
                margin: const EdgeInsets.symmetric(horizontal: 3),
                padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                decoration: BoxDecoration(
                  color: Colors.orange.withValues(alpha: 0.2),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                    color: Colors.orange.withValues(alpha: 0.5),
                  ),
                ),
                child: Text(
                  '${provider.items.where((e) => e.status == 'Pending').length} pendientes',
                  style: const TextStyle(
                    color: Colors.orange,
                    fontWeight: FontWeight.bold,
                    fontSize: 8,
                  ),
                ),
              ),
            ),
          IconButton(
            tooltip: provider.ttsEnabled ? 'Silenciar voz' : 'Activar voz',
            icon: Icon(
              provider.ttsEnabled
                  ? Icons.volume_up_rounded
                  : Icons.volume_off_rounded,
              color: provider.ttsEnabled
                  ? const Color(0xFF4EE87A)
                  : Colors.white30,
              size: 15,
            ),
            onPressed: () => context.read<StationProvider>().setTtsEnabled(
              !provider.ttsEnabled,
            ),
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints.tightFor(width: 30, height: 30),
          ),
          PopupMenuButton<String>(
            icon: const Icon(Icons.more_vert, color: Colors.white54, size: 15),
            padding: EdgeInsets.zero,
            color: const Color(0xFF1C1C3A),
            onSelected: (value) {
              if (value == 'change') {
                context.read<StationProvider>().changeStation();
              }
              if (value == 'logout') context.read<StationProvider>().logout();
            },
            itemBuilder: (_) => [
              const PopupMenuItem(
                value: 'change',
                child: Row(
                  children: [
                    Icon(Icons.swap_horiz, color: Colors.white70, size: 13),
                    SizedBox(width: 4),
                    Text(
                      'Cambiar estación',
                      style: TextStyle(color: Colors.white, fontSize: 10),
                    ),
                  ],
                ),
              ),
              const PopupMenuItem(
                value: 'logout',
                child: Row(
                  children: [
                    Icon(Icons.logout, color: Colors.redAccent, size: 13),
                    SizedBox(width: 4),
                    Text(
                      'Cerrar sesión',
                      style: TextStyle(color: Colors.redAccent, fontSize: 10),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(width: 2),
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
                const CompletedSidebar(width: 140),
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
  const _ErrorBanner({
    required this.message,
    required this.onRetry,
    required this.onDismiss,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      color: const Color(0xFF3D0000),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Row(
        children: [
          const Icon(
            Icons.warning_amber_rounded,
            color: Colors.redAccent,
            size: 11,
          ),
          const SizedBox(width: 5),
          Expanded(
            child: Text(
              message,
              style: const TextStyle(color: Colors.white70, fontSize: 9),
              overflow: TextOverflow.ellipsis,
            ),
          ),
          TextButton(
            onPressed: onRetry,
            child: const Text(
              'Reintentar',
              style: TextStyle(color: Colors.orangeAccent, fontSize: 9),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.close, color: Colors.white38, size: 11),
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
    if (provider.connectionState == HubConnectionState.Reconnecting &&
        provider.items.isEmpty) {
      return const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              width: 18,
              height: 18,
              child: CircularProgressIndicator(
                color: Color(0xFFE94560),
                strokeWidth: 2,
              ),
            ),
            SizedBox(height: 7),
            Text(
              'Reconectando...',
              style: TextStyle(color: Colors.white54, fontSize: 10),
            ),
          ],
        ),
      );
    }

    final groups = provider.orderedGroups;

    if (groups.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.check_circle_outline,
              color: Color(0xFF2ECC71),
              size: 32,
            ),
            const SizedBox(height: 7),
            Text(
              provider.connectionState == HubConnectionState.Connected
                  ? 'Todo listo'
                  : 'Sin conexión',
              style: const TextStyle(
                color: Colors.white70,
                fontSize: 14,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 3),
            const Text(
              'Los nuevos pedidos aparecerán aquí.',
              style: TextStyle(color: Colors.white38, fontSize: 9),
            ),
          ],
        ),
      );
    }

    // Wrap: los pedidos más antiguos quedan a la izquierda,
    // los nuevos se agregan a la derecha, y envuelven hacia abajo.
    return SingleChildScrollView(
      padding: const EdgeInsets.all(5),
      child: Wrap(
        spacing: 5,
        runSpacing: 5,
        alignment: WrapAlignment.start,
        crossAxisAlignment: WrapCrossAlignment.start,
        children: groups
            .map((entry) => OrderCard(orderId: entry.key, items: entry.value))
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
          width: 5,
          height: 5,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 3),
        Text(label, style: TextStyle(color: color, fontSize: 8)),
      ],
    );
  }
}
