import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/services/overlay_bubble_service.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../auth/presentation/providers/auth_controller.dart';
import '../../data/models/table_service_models.dart';
import '../providers/table_requests_controller.dart';

int _requestTableNumber(String code) => int.tryParse(code.trim()) ?? 0x3fffffff;

int _compareRequestsByTable(TableServiceRequest a, TableServiceRequest b) {
  final numberCompare = _requestTableNumber(
    a.tableCode,
  ).compareTo(_requestTableNumber(b.tableCode));
  if (numberCompare != 0) return numberCompare;

  final codeCompare = a.tableCode.toLowerCase().compareTo(
    b.tableCode.toLowerCase(),
  );
  if (codeCompare != 0) return codeCompare;

  final areaCompare = (a.tableArea ?? '').toLowerCase().compareTo(
    (b.tableArea ?? '').toLowerCase(),
  );
  if (areaCompare != 0) return areaCompare;

  return a.requestedAt.compareTo(b.requestedAt);
}

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
    return switch (status) {
      TableServiceRequestStatus.pending => const Color(0xFFFFAB00),
      TableServiceRequestStatus.taken => const Color(0xFF40C4FF),
      TableServiceRequestStatus.inProgress => const Color(0xFF40C4FF),
      TableServiceRequestStatus.completed => const Color(0xFF69F0AE),
      TableServiceRequestStatus.cancelled => const Color(0xFF9E9E9E),
    };
  }

  String _statusLabel(TableServiceRequestStatus status) {
    return switch (status) {
      TableServiceRequestStatus.pending => 'Pendiente',
      TableServiceRequestStatus.taken => 'Tomada',
      TableServiceRequestStatus.inProgress => 'En proceso',
      TableServiceRequestStatus.completed => 'Completada',
      TableServiceRequestStatus.cancelled => 'Cancelada',
    };
  }

  // ── Build principal ────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(tableRequestsControllerProvider);

    final activeRequests =
        state.requests
            .where(
              (r) =>
                  r.status != TableServiceRequestStatus.completed &&
                  r.status != TableServiceRequestStatus.cancelled,
            )
            .toList()
          ..sort(_compareRequestsByTable);

    final historyRequests =
        state.requests
            .where(
              (r) =>
                  r.status == TableServiceRequestStatus.completed ||
                  r.status == TableServiceRequestStatus.cancelled,
            )
            .toList()
          ..sort(_compareRequestsByTable);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Solicitudes'),
        actions: [
          IconButton(
            icon: const Icon(Icons.picture_in_picture_alt_rounded),
            tooltip: 'Burbuja flotante',
            onPressed: _activateOverlayBubble,
          ),
          IconButton(
            icon: const Icon(Icons.logout_rounded),
            tooltip: 'Cerrar sesión',
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
                    _CountBadge(count: historyRequests.length, dimmed: true),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
      body: Column(
        children: [
          _buildConnectionBanner(state),
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                _buildActiveTab(state, activeRequests),
                _buildHistoryTab(state, historyRequests),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ── Tab activas ────────────────────────────────────────────────────────────

  Widget _buildActiveTab(
    TableRequestsState state,
    List<TableServiceRequest> activeRequests,
  ) {
    if (state.isLoading && state.requests.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.errorMessage != null && state.requests.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.error_outline_rounded,
                size: 56,
                color: kGold.withAlpha(160),
              ),
              const SizedBox(height: 16),
              Text(
                state.errorMessage!,
                textAlign: TextAlign.center,
                style: GoogleFonts.lato(color: kParchmentDim, fontSize: 14),
              ),
              const SizedBox(height: 24),
              OutlinedButton.icon(
                onPressed: () => ref
                    .read(tableRequestsControllerProvider.notifier)
                    .loadRequests(),
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Reintentar'),
              ),
            ],
          ),
        ),
      );
    }

    if (activeRequests.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: kBgCard,
                shape: BoxShape.circle,
                border: Border.all(color: kGoldDark.withAlpha(80), width: 1),
              ),
              child: Icon(
                Icons.auto_awesome_outlined,
                size: 40,
                color: kGold.withAlpha(160),
              ),
            ),
            const SizedBox(height: 20),
            Text(
              'Sin solicitudes activas',
              style: GoogleFonts.cinzel(
                color: kParchment,
                fontSize: 16,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Las mesas están tranquilas por ahora.',
              style: GoogleFonts.lato(color: kParchmentDim, fontSize: 13),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () =>
          ref.read(tableRequestsControllerProvider.notifier).loadRequests(),
      child: ListView.builder(
        padding: const EdgeInsets.fromLTRB(12, 10, 12, 16),
        itemCount: activeRequests.length,
        itemBuilder: (_, i) => _buildActiveCard(activeRequests[i]),
      ),
    );
  }

  // ── Tab historial ──────────────────────────────────────────────────────────

  Widget _buildHistoryTab(
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
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: kBgCard,
                shape: BoxShape.circle,
                border: Border.all(color: kGoldDark.withAlpha(60), width: 1),
              ),
              child: Icon(
                Icons.history_rounded,
                size: 40,
                color: kParchmentDim.withAlpha(160),
              ),
            ),
            const SizedBox(height: 20),
            Text(
              'Sin historial reciente',
              style: GoogleFonts.cinzel(
                color: kParchment,
                fontSize: 16,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Aquí aparecen las últimas 24 horas.',
              style: GoogleFonts.lato(color: kParchmentDim, fontSize: 13),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () =>
          ref.read(tableRequestsControllerProvider.notifier).loadRequests(),
      child: ListView.builder(
        padding: const EdgeInsets.fromLTRB(12, 10, 12, 16),
        itemCount: historyRequests.length,
        itemBuilder: (_, i) => _buildHistoryCard(historyRequests[i]),
      ),
    );
  }

  // ── Card solicitud activa ──────────────────────────────────────────────────

  Widget _buildActiveCard(TableServiceRequest request) {
    final statusColor = _statusColor(request.status);
    final actionLabel = switch (request.status) {
      TableServiceRequestStatus.pending => 'Tomar solicitud',
      TableServiceRequestStatus.taken => 'Iniciar atención',
      TableServiceRequestStatus.inProgress => 'Completar solicitud',
      _ => null,
    };
    final actionIcon = switch (request.status) {
      TableServiceRequestStatus.pending => Icons.play_arrow_rounded,
      TableServiceRequestStatus.taken => Icons.bolt_rounded,
      TableServiceRequestStatus.inProgress => Icons.check_rounded,
      _ => null,
    };

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Tira de color izquierda según estado
              Container(width: 4, color: statusColor),
              // Contenido
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(14),
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
                                  style: GoogleFonts.cinzel(
                                    color: kParchment,
                                    fontSize: 16,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                const SizedBox(height: 5),
                                Text(
                                  request.displayDescription,
                                  style: GoogleFonts.lato(
                                    color: kParchment,
                                    fontSize: 14,
                                  ),
                                ),
                                const SizedBox(height: 5),
                                _MetaText(
                                  icon: Icons.access_time_rounded,
                                  text: _formatTime(request.requestedAt),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(width: 10),
                          _StatusChip(
                            label: _statusLabel(request.status),
                            color: statusColor,
                          ),
                        ],
                      ),
                      if (actionLabel != null && actionIcon != null) ...[
                        const SizedBox(height: 12),
                        SizedBox(
                          width: double.infinity,
                          child: FilledButton.icon(
                            onPressed: () {
                              final ctrl = ref.read(
                                tableRequestsControllerProvider.notifier,
                              );
                              switch (request.status) {
                                case TableServiceRequestStatus.pending:
                                  ctrl.takeRequest(request.id);
                                case TableServiceRequestStatus.taken:
                                  ctrl.startRequest(request.id);
                                case TableServiceRequestStatus.inProgress:
                                  ctrl.completeRequest(request.id);
                                default:
                                  break;
                              }
                            },
                            icon: Icon(actionIcon, size: 18),
                            label: Text(actionLabel),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // ── Card historial ─────────────────────────────────────────────────────────

  Widget _buildHistoryCard(TableServiceRequest request) {
    final isCompleted = request.status == TableServiceRequestStatus.completed;
    final statusColor = _statusColor(request.status);
    final resolvedAt = request.completedAt ?? request.requestedAt;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      color: kBgMid,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              margin: const EdgeInsets.only(top: 2, right: 12),
              decoration: BoxDecoration(
                color: statusColor.withAlpha(30),
                shape: BoxShape.circle,
              ),
              padding: const EdgeInsets.all(8),
              child: Icon(
                isCompleted
                    ? Icons.check_circle_outline_rounded
                    : Icons.cancel_outlined,
                color: statusColor,
                size: 20,
              ),
            ),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          request.tableName,
                          style: GoogleFonts.cinzel(
                            color: kParchment,
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      _StatusChip(
                        label: _statusLabel(request.status),
                        color: statusColor,
                        small: true,
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    request.displayDescription,
                    style: GoogleFonts.lato(color: kParchmentDim, fontSize: 13),
                  ),
                  const SizedBox(height: 6),
                  Wrap(
                    spacing: 12,
                    runSpacing: 2,
                    children: [
                      _MetaText(
                        icon: Icons.access_time_rounded,
                        text: _formatTime(resolvedAt),
                      ),
                      if (request.takenByName != null)
                        _MetaText(
                          icon: Icons.person_outline_rounded,
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

  Widget _buildConnectionBanner(TableRequestsState state) {
    final (icon, bg, fg, label) = switch (state.connectionStatus) {
      TableRequestsConnectionStatus.connecting => (
        Icons.sync_rounded,
        const Color(0xFFFFAB00).withAlpha(22),
        const Color(0xFFFFAB00),
        'Conectando tiempo real...',
      ),
      TableRequestsConnectionStatus.connected => (
        Icons.wifi_tethering_rounded,
        const Color(0xFF69F0AE).withAlpha(20),
        const Color(0xFF69F0AE),
        'Tiempo real activo · Actualización automática',
      ),
      TableRequestsConnectionStatus.degraded => (
        Icons.cloud_off_rounded,
        const Color(0xFFFF6B6B).withAlpha(22),
        const Color(0xFFFF6B6B),
        'Modo respaldo · Recarga periódica',
      ),
    };

    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: fg.withAlpha(50), width: 1),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: fg, size: 15),
              const SizedBox(width: 7),
              Text(
                label,
                style: GoogleFonts.lato(
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
        messenger.showSnackBar(
          const SnackBar(
            content: Text(
              'Habilita "Mostrar sobre otras apps" y vuelve a tocar el botón.',
            ),
          ),
        );
        return;
      }
      await _overlayBubbleService.showBubble();
      if (!mounted) return;
      messenger.showSnackBar(
        const SnackBar(
          content: Text('Burbuja activada. Tócala para volver a la app.'),
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
        horizontal: small ? 8 : 10,
        vertical: small ? 3 : 5,
      ),
      decoration: BoxDecoration(
        color: color.withAlpha(35),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withAlpha(80), width: 1),
      ),
      child: Text(
        label,
        style: GoogleFonts.lato(
          color: color,
          fontWeight: FontWeight.w700,
          fontSize: small ? 11 : 12,
        ),
      ),
    );
  }
}

class _CountBadge extends StatelessWidget {
  const _CountBadge({required this.count, this.dimmed = false});

  final int count;
  final bool dimmed;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
      decoration: BoxDecoration(
        color: dimmed ? kParchmentDim.withAlpha(60) : kGold,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Text(
        '$count',
        style: GoogleFonts.lato(
          color: dimmed ? kParchmentDim : kBrown,
          fontSize: 11,
          fontWeight: FontWeight.w800,
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
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 13, color: kParchmentDim.withAlpha(160)),
        const SizedBox(width: 3),
        Text(
          text,
          style: GoogleFonts.lato(
            fontSize: 12,
            color: kParchmentDim.withAlpha(160),
          ),
        ),
      ],
    );
  }
}
