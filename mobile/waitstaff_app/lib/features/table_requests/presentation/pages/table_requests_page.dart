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

class _TableRequestsPageState extends ConsumerState<TableRequestsPage> {
  final OverlayBubbleService _overlayBubbleService = OverlayBubbleService();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(tableRequestsControllerProvider.notifier).initialize();
    });
  }

  Color _getStatusColor(TableServiceRequestStatus status) {
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

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(tableRequestsControllerProvider);
    final activeRequests = state.requests
        .where(
          (r) =>
              r.status != TableServiceRequestStatus.completed &&
              r.status != TableServiceRequestStatus.cancelled,
        )
      .toList(growable: false)
      .cast<TableServiceRequest>();

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
              if (!context.mounted) {
                return;
              }
              context.go('/login');
            },
          ),
        ],
      ),
      body: Column(
        children: [
          _buildConnectionBanner(context, state),
          Expanded(
            child: _buildBody(context, state, activeRequests),
          ),
        ],
      ),
    );
  }

  Future<void> _activateOverlayBubble() async {
    final messenger = ScaffoldMessenger.of(context);

    try {
      final hasPermission = await _overlayBubbleService.canDrawOverlays();
      if (!hasPermission) {
        await _overlayBubbleService.requestOverlayPermission();
        messenger.showSnackBar(
          const SnackBar(
            content: Text(
              'Habilita "Mostrar sobre otras apps" y vuelve a tocar el boton de burbuja.',
            ),
          ),
        );
        return;
      }

      await _overlayBubbleService.showBubble();

      if (!mounted) {
        return;
      }

      messenger.showSnackBar(
        const SnackBar(
          content: Text('Burbuja activada. Toca la burbuja para volver a abrir la app.'),
        ),
      );
    } on PlatformException {
      messenger.showSnackBar(
        const SnackBar(
          content: Text('No se pudo activar la burbuja flotante.'),
        ),
      );
    }
  }

  Widget _buildBody(
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
            Icon(
              Icons.error_outline,
              size: 64,
              color: Theme.of(context).colorScheme.error,
            ),
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
            Icon(
              Icons.check_circle,
              size: 64,
              color: Theme.of(context).colorScheme.primary,
            ),
            const SizedBox(height: 16),
            Text(
              'Sin solicitudes pendientes',
              style: Theme.of(context).textTheme.titleLarge,
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
        itemCount: activeRequests.length,
        itemBuilder: (context, index) {
          return _buildRequestCard(context, activeRequests[index]);
        },
      ),
    );
  }

  Widget _buildConnectionBanner(
    BuildContext context,
    TableRequestsState state,
  ) {
    final colorScheme = Theme.of(context).colorScheme;

    IconData icon;
    Color backgroundColor;
    Color foregroundColor;
    String label;
    String subtitle;

    switch (state.connectionStatus) {
      case TableRequestsConnectionStatus.connecting:
        icon = Icons.sync;
        backgroundColor = Colors.orange.withAlpha(26);
        foregroundColor = Colors.orange.shade800;
        label = 'Conectando tiempo real';
        subtitle = 'Conectando...';
      case TableRequestsConnectionStatus.connected:
        icon = Icons.wifi_tethering;
        backgroundColor = Colors.green.withAlpha(24);
        foregroundColor = Colors.green.shade800;
        label = 'Tiempo real activo';
        subtitle = 'Actualizacion automatica';
      case TableRequestsConnectionStatus.degraded:
        icon = Icons.cloud_off;
        backgroundColor = colorScheme.errorContainer.withAlpha(140);
        foregroundColor = colorScheme.onErrorContainer;
        label = 'Conexion en modo respaldo';
        subtitle = 'Recarga periodica';
    }

    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 6, 8, 4),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: backgroundColor,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          child: Row(
            children: [
              Icon(icon, color: foregroundColor, size: 18),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  '$label · $subtitle',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: foregroundColor,
                    fontWeight: FontWeight.w600,
                    fontSize: 12,
                  ),
                ),
              ),
              const SizedBox(width: 26),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildRequestCard(
    BuildContext context,
    TableServiceRequest request,
  ) {
    final statusColor = _getStatusColor(request.status);
    final actionLabel = switch (request.status) {
      TableServiceRequestStatus.pending => 'Tomar solicitud',
      TableServiceRequestStatus.taken || TableServiceRequestStatus.inProgress =>
        'Completar solicitud',
      _ => null,
    };

    final actionIcon = switch (request.status) {
      TableServiceRequestStatus.pending => Icons.play_arrow,
      TableServiceRequestStatus.taken || TableServiceRequestStatus.inProgress =>
        Icons.check,
      _ => null,
    };

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
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
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        request.displayDescription,
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                      const SizedBox(height: 8),
                      Text(
                        _formatTime(request.requestedAt),
                        style: Theme.of(context).textTheme.labelSmall?.copyWith(
                          color: Theme.of(context).colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: statusColor.withAlpha(51),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    _statusLabel(request.status),
                    style: TextStyle(
                      color: statusColor,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
            if (actionLabel != null && actionIcon != null) ...[
              const SizedBox(height: 14),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () {
                    final controller = ref.read(
                      tableRequestsControllerProvider.notifier,
                    );
                    if (request.status == TableServiceRequestStatus.pending) {
                      controller.takeRequest(request.id);
                    } else if (request.status == TableServiceRequestStatus.taken ||
                        request.status == TableServiceRequestStatus.inProgress) {
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

  String _formatTime(DateTime dateTime) {
    final now = DateTime.now();
    final difference = now.difference(dateTime);

    if (difference.inSeconds < 60) {
      return 'Hace unos segundos';
    } else if (difference.inMinutes < 60) {
      return 'Hace ${difference.inMinutes} min';
    } else if (difference.inHours < 24) {
      return 'Hace ${difference.inHours} horas';
    } else {
      return 'Hace ${difference.inDays} días';
    }
  }
}
