import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/services/overlay_bubble_service.dart';
import '../../../auth/presentation/providers/auth_controller.dart';
import '../../data/models/table_service_models.dart';
import '../providers/table_requests_controller.dart';

class TableRequestsPage extends ConsumerStatefulWidget {
  const TableRequestsPage({super.key});

  @override
  ConsumerState<TableRequestsPage> createState() => _TableRequestsPageState();
}

class _TableRequestsPageState extends ConsumerState<TableRequestsPage>
    with TickerProviderStateMixin {
  final OverlayBubbleService _overlayBubbleService = OverlayBubbleService();
  late final TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(tableRequestsControllerProvider.notifier).initialize();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  // ── Colores y etiquetas de estado ──────────────────────────────────────────

  Color _statusColor(TableServiceRequestStatus status) {
    switch (status) {
      case TableServiceRequestStatus.pending:
        return Colors.orange;
      case TableServiceRequestStatus.taken:
      case TableServiceRequestStatus.inProgress:
        return Colors.blue;
      case TableServiceRequestStatus.completed:
        return Colors.green;
      case TableServiceRequestStatus.cancelled:
        return Colors.grey;
    }
  }

  String _statusLabel(TableServiceRequestStatus status) {
    switch (status) {
      case TableServiceRequestStatus.pending:
        return 'Pendiente';
      case TableServiceRequestStatus.taken:
        return 'Tomada';
      case TableServiceRequestStatus.inProgress:
        return 'En proceso';
      case TableServiceRequestStatus.completed:
        return 'Completada';
      case TableServiceRequestStatus.cancelled:
        return 'Cancelada';
    }
  }

  // ── Build principal ────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(tableRequestsControllerProvider);

    final activeRequests = state.requests
        .where((r) =>
            r.status != TableServiceRequestStatus.completed &&
            r.status != TableServiceRequestStatus.cancelled)
        .toList(growable: false);

    final historyRequests = state.requests
        .where((r) =>
            r.status == TableServiceRequestStatus.completed ||
            r.status == TableServiceRequestStatus.cancelled)
        .toList(growable: false);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Solicitudes de Mesa'),
        elevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.picture_in_picture_alt),
            tooltip: 'Activar burbuja flotante',
            onPressed: _activateOverlayBubble,
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () async {
              await ref.read(authControllerProvider.notifier).logout();
              if (!context.mounted) return;
              context.go('/login');
            },
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            Tab(
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text('Activas'),
                  if (activeRequests.isNotEmpty) ...[
                    const SizedBox(width: 6),
                    _CountBadge(count: activeRequests.length),
                  ],
                ],
              ),
            ),
            Tab(
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text('Historial'),
                  if (historyRequests.isNotEmpty) ...[
                    const SizedBox(width: 6),
                    _CountBadge(
                      count: historyRequests.length,
                      color: Colors.grey,
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
      body: Column(
        children: [
          _buildConnectionBanner(context, state),
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                _buildActiveTab(context, state, activeRequests),
                _buildHistoryTab(context, state, historyRequests),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ── Tab activas ────────────────────────────────────────────────────────────

  Widget _buildActiveTab(
    BuildContext context,
    TableRequestsState state,
    List<TableServiceRequest> activeRequests,
  ) {
    if (state.isLoading && state.requests.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.errorMessage != null && state.requests.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.error_outline,
                size: 64, color: Theme.of(context).colorScheme.error),
            const SizedBox(height: 16),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 32),
              child: Text(
                state.errorMessage!,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: () =>
                  ref.read(tableRequestsControllerProvider.notifier).loadRequests(),
              icon: const Icon(Icons.refresh),
              label: const Text('Reintentar'),
            ),
          ],
        ),
      );
    }

    if (activeRequests.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.check_circle,
                size: 64, color: Theme.of(context).colorScheme.primary),
            const SizedBox(height: 16),
            Text('Sin solicitudes pendientes',
                style: Theme.of(context).textTheme.titleLarge),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () =>
          ref.read(tableRequestsControllerProvider.notifier).loadRequests(),
      child: ListView.builder(
        padding: const EdgeInsets.all(8),
        itemCount: activeRequests.length,
        itemBuilder: (context, index) =>
            _buildActiveCard(context, activeRequests[index]),
      ),
    );
  }

  // ── Tab historial ──────────────────────────────────────────────────────────

  Widget _buildHistoryTab(
    BuildContext context,
    TableRequestsState state,
    List<TableServiceRequest> historyRequests,
  ) {
    if (state.isLoading && state.requests.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (historyRequests.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.history,
                size: 64,
                color: Theme.of(context).colorScheme.onSurfaceVariant),
            const SizedBox(height: 16),
            Text(
              'Sin historial en las últimas 24h',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 8),
            Text(
              'Aquí aparecen las solicitudes completadas o canceladas.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () =>
          ref.read(tableRequestsControllerProvider.notifier).loadRequests(),
      child: ListView.builder(
        padding: const EdgeInsets.all(8),
        itemCount: historyRequests.length,
        itemBuilder: (context, index) =>
            _buildHistoryCard(context, historyRequests[index]),
      ),
    );
  }

  // ── Card solicitud activa ──────────────────────────────────────────────────

  Widget _buildActiveCard(BuildContext context, TableServiceRequest request) {
    final statusColor = _statusColor(request.status);
    final actionLabel = switch (request.status) {
      TableServiceRequestStatus.pending => 'Tomar solicitud',
      TableServiceRequestStatus.taken ||
      TableServiceRequestStatus.inProgress =>
        'Completar solicitud',
      _ => null,
    };
    final actionIcon = switch (request.status) {
      TableServiceRequestStatus.pending => Icons.play_arrow,
      TableServiceRequestStatus.taken ||
      TableServiceRequestStatus.inProgress =>
        Icons.check,
      _ => null,
    };

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        request.tableName,
                        style: Theme.of(context)
                            .textTheme
                            .titleLarge
                            ?.copyWith(fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 6),
                      Text(request.displayDescription,
                          style: Theme.of(context).textTheme.bodyMedium),
                      const SizedBox(height: 6),
                      Text(
                        _formatTime(request.requestedAt),
                        style: Theme.of(context).textTheme.labelSmall?.copyWith(
                              color: Theme.of(context)
                                  .colorScheme
                                  .onSurfaceVariant,
                            ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                _StatusChip(
                    label: _statusLabel(request.status), color: statusColor),
              ],
            ),
            if (actionLabel != null && actionIcon != null) ...[
              const SizedBox(height: 14),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () {
                    final controller =
                        ref.read(tableRequestsControllerProvider.notifier);
                    if (request.status == TableServiceRequestStatus.pending) {
                      controller.takeRequest(request.id);
                    } else {
                      controller.completeRequest(request.id);
                    }
                  },
                  icon: Icon(actionIcon),
                  label: Text(actionLabel),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  // ── Card historial ─────────────────────────────────────────────────────────

  Widget _buildHistoryCard(BuildContext context, TableServiceRequest request) {
    final isCompleted =
        request.status == TableServiceRequestStatus.completed;
    final statusColor = _statusColor(request.status);
    final resolvedAt = request.completedAt ?? request.requestedAt;

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      color: Theme.of(context).colorScheme.surfaceContainerLow,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Ícono de estado
            Container(
              margin: const EdgeInsets.only(top: 2, right: 12),
              decoration: BoxDecoration(
                color: statusColor.withAlpha(30),
                shape: BoxShape.circle,
              ),
              padding: const EdgeInsets.all(8),
              child: Icon(
                isCompleted ? Icons.check_circle : Icons.cancel,
                color: statusColor,
                size: 20,
              ),
            ),

            // Contenido
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Mesa + chip de estado
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          request.tableName,
                          style: Theme.of(context)
                              .textTheme
                              .titleMedium
                              ?.copyWith(fontWeight: FontWeight.w600),
                        ),
                      ),
                      _StatusChip(
                          label: _statusLabel(request.status),
                          color: statusColor,
                          small: true),
                    ],
                  ),
                  const SizedBox(height: 4),

                  // Descripción de la solicitud
                  Text(
                    request.displayDescription,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 6),

                  // Fila de metadatos
                  Wrap(
                    spacing: 12,
                    runSpacing: 2,
                    children: [
                      _MetaText(
                        icon: Icons.access_time,
                        text: _formatTime(resolvedAt),
                      ),
                      if (request.takenByName != null)
                        _MetaText(
                          icon: Icons.person_outline,
                          text: request.takenByName!,
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ── Banner de conexión ─────────────────────────────────────────────────────

  Widget _buildConnectionBanner(
      BuildContext context, TableRequestsState state) {
    final colorScheme = Theme.of(context).colorScheme;

    final (icon, bg, fg, label) = switch (state.connectionStatus) {
      TableRequestsConnectionStatus.connecting => (
          Icons.sync,
          Colors.orange.withAlpha(26),
          Colors.orange.shade800,
          'Conectando tiempo real...',
        ),
      TableRequestsConnectionStatus.connected => (
          Icons.wifi_tethering,
          Colors.green.withAlpha(24),
          Colors.green.shade800,
          'Tiempo real activo · Actualización automática',
        ),
      TableRequestsConnectionStatus.degraded => (
          Icons.cloud_off,
          colorScheme.errorContainer.withAlpha(140),
          colorScheme.onErrorContainer,
          'Modo respaldo · Recarga periódica',
        ),
    };

    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 6, 8, 4),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: fg, size: 16),
              const SizedBox(width: 6),
              Text(
                label,
                style: TextStyle(
                  color: fg,
                  fontWeight: FontWeight.w600,
                  fontSize: 12,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // ── Overlay burbuja ────────────────────────────────────────────────────────

  Future<void> _activateOverlayBubble() async {
    final messenger = ScaffoldMessenger.of(context);
    try {
      final hasPermission = await _overlayBubbleService.canDrawOverlays();
      if (!hasPermission) {
        await _overlayBubbleService.requestOverlayPermission();
        messenger.showSnackBar(const SnackBar(
          content: Text(
              'Habilita "Mostrar sobre otras apps" y vuelve a tocar el botón.'),
        ));
        return;
      }
      await _overlayBubbleService.showBubble();
      if (!mounted) return;
      messenger.showSnackBar(const SnackBar(
        content: Text('Burbuja activada. Tócala para volver a la app.'),
      ));
    } on PlatformException {
      messenger.showSnackBar(const SnackBar(
        content: Text('No se pudo activar la burbuja flotante.'),
      ));
    }
  }

  // ── Utilidades ─────────────────────────────────────────────────────────────

  String _formatTime(DateTime dateTime) {
    final diff = DateTime.now().difference(dateTime);
    if (diff.inSeconds < 60) return 'Hace unos segundos';
    if (diff.inMinutes < 60) return 'Hace ${diff.inMinutes} min';
    if (diff.inHours < 24) return 'Hace ${diff.inHours} h';
    return 'Hace ${diff.inDays} días';
  }
}

// ── Widgets auxiliares ─────────────────────────────────────────────────────

class _StatusChip extends StatelessWidget {
  const _StatusChip({
    required this.label,
    required this.color,
    this.small = false,
  });

  final String label;
  final Color color;
  final bool small;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: small ? 8 : 12,
        vertical: small ? 3 : 6,
      ),
      decoration: BoxDecoration(
        color: color.withAlpha(40),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontWeight: FontWeight.w600,
          fontSize: small ? 11 : 13,
        ),
      ),
    );
  }
}

class _CountBadge extends StatelessWidget {
  const _CountBadge({required this.count, this.color});

  final int count;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final bg = color ?? Theme.of(context).colorScheme.primary;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Text(
        '$count',
        style: TextStyle(
          color: color != null
              ? Colors.white
              : Theme.of(context).colorScheme.onPrimary,
          fontSize: 11,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }
}

class _MetaText extends StatelessWidget {
  const _MetaText({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.onSurfaceVariant;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 13, color: color),
        const SizedBox(width: 3),
        Text(
          text,
          style: TextStyle(fontSize: 12, color: color),
        ),
      ],
    );
  }
}
