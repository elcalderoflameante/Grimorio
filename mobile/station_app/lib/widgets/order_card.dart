import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/station_item.dart';
import '../providers/station_provider.dart';
import 'display_size_scope.dart';

// Formato compacto para mostrar más pedidos sin ocultar nombres de combos.
const double kCardWidth = 170.0;
const double kCardHeight = 190.0;

class OrderCard extends StatelessWidget {
  final String orderId;
  final List<StationItem> items;

  const OrderCard({super.key, required this.orderId, required this.items});

  String _orderTitle() {
    final first = items.first;
    if (first.tableCode != null) {
      return 'Pedido #${first.orderNumber} - Mesa ${first.tableCode}';
    }
    if (first.customerName != null) {
      return 'Pedido #${first.orderNumber} - ${first.customerName!}';
    }
    return 'Pedido #${first.orderNumber}';
  }

  String _orderTypeBadge() {
    final t = items.first.orderType.toLowerCase();
    if (t.contains('mesa') || t.contains('dine')) return 'Mesa';
    if (t.contains('llevar') || t.contains('takeout')) return 'Llevar';
    if (t.contains('delivery')) return 'Delivery';
    return items.first.orderType;
  }

  Color _typeBadgeColor() {
    final t = items.first.orderType.toLowerCase();
    if (t.contains('mesa') || t.contains('dine')) {
      return const Color(0xFF3B82F6);
    }
    if (t.contains('llevar') || t.contains('takeout')) {
      return const Color(0xFFF59E0B);
    }
    if (t.contains('delivery')) return const Color(0xFF8B5CF6);
    return Colors.grey;
  }

  String _elapsed() {
    final mins = DateTime.now().difference(items.first.confirmedAt).inMinutes;
    if (mins < 1) return '< 1 min';
    return '$mins min';
  }

  Color _elapsedColor() {
    final mins = DateTime.now().difference(items.first.confirmedAt).inMinutes;
    if (mins < 10) return Colors.white38;
    if (mins < 20) return Colors.orange;
    return Colors.redAccent;
  }

  // Indicador de urgencia en el borde de la tarjeta
  Color _cardBorderColor() {
    final mins = DateTime.now().difference(items.first.confirmedAt).inMinutes;
    final hasPending = items.any((i) => i.status == 'Pending');
    if (hasPending && mins >= 20) return Colors.redAccent;
    if (hasPending && mins >= 10) return Colors.orange;
    return const Color(0xFF2A2A4A);
  }

  String? _orderNotes() {
    final notes = items.first.orderNotes?.trim();
    if (notes == null || notes.isEmpty) return null;
    return notes;
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      height: context.displaySize(kCardHeight),
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: const Color(0xFF1C1C30),
        borderRadius: BorderRadius.circular(5),
        border: Border.all(color: _cardBorderColor()),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // ── Header ───────────────────────────────────────────────────
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
            decoration: const BoxDecoration(
              color: Color(0xFF12122A),
              borderRadius: BorderRadius.vertical(top: Radius.circular(4)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  _orderTitle(),
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.bold,
                    fontSize: 10,
                  ),
                  maxLines: 2,
                  softWrap: true,
                ),
                const SizedBox(height: 3),
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 3,
                        vertical: 1,
                      ),
                      decoration: BoxDecoration(
                        color: _typeBadgeColor().withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(2),
                        border: Border.all(
                          color: _typeBadgeColor().withValues(alpha: 0.5),
                        ),
                      ),
                      child: Text(
                        _orderTypeBadge(),
                        style: TextStyle(
                          color: _typeBadgeColor(),
                          fontSize: 8,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    const Spacer(),
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.schedule, size: 8, color: _elapsedColor()),
                        const SizedBox(width: 2),
                        Text(
                          _elapsed(),
                          style: TextStyle(color: _elapsedColor(), fontSize: 8),
                        ),
                      ],
                    ),
                  ],
                ),
              ],
            ),
          ),

          Expanded(
            child: _OrderDetails(items: items, orderNotes: _orderNotes()),
          ),
        ],
      ),
    );
  }
}

class _OrderDetails extends StatefulWidget {
  const _OrderDetails({required this.items, required this.orderNotes});

  final List<StationItem> items;
  final String? orderNotes;

  @override
  State<_OrderDetails> createState() => _OrderDetailsState();
}

class _OrderDetailsState extends State<_OrderDetails> {
  final ScrollController _controller = ScrollController();
  late String _itemsSignature;

  String _signature(List<StationItem> items) =>
      items.map((item) => '${item.orderItemId}:${item.status}').join('|');

  @override
  void initState() {
    super.initState();
    _itemsSignature = _signature(widget.items);
  }

  @override
  void didUpdateWidget(covariant _OrderDetails oldWidget) {
    super.didUpdateWidget(oldWidget);
    final nextSignature = _signature(widget.items);
    if (nextSignature == _itemsSignature) return;
    _itemsSignature = nextSignature;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted && _controller.hasClients) _controller.jumpTo(0);
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return RawScrollbar(
      controller: _controller,
      thumbVisibility: true,
      thumbColor: Colors.white38,
      thickness: 3,
      radius: Radius.zero,
      crossAxisMargin: 0,
      mainAxisMargin: 3,
      padding: EdgeInsets.zero,
      scrollbarOrientation: ScrollbarOrientation.right,
      child: ListView(
        controller: _controller,
        primary: false,
        padding: const EdgeInsets.fromLTRB(3, 3, 7, 3),
        children: [
          if (widget.orderNotes != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(3, 1, 5, 4),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(
                    Icons.info_outline_rounded,
                    color: Colors.amberAccent,
                    size: 9,
                  ),
                  const SizedBox(width: 3),
                  Expanded(
                    child: Text(
                      widget.orderNotes!,
                      style: const TextStyle(
                        color: Colors.amberAccent,
                        fontSize: 9,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ...widget.items.map((item) => _ItemRow(item: item)),
        ],
      ),
    );
  }
}

// ── Fila de ítem ─────────────────────────────────────────────────────────────

class _ItemRow extends StatelessWidget {
  final StationItem item;
  const _ItemRow({required this.item});

  // Strings exactos del enum C# OrderItemStatus.ToString()
  Color _bg(String status) => switch (status) {
    'Pending' => const Color(0xFF3D1800),
    'InPreparation' => const Color(0xFF00204A),
    'Ready' => const Color(0xFF003D1A),
    _ => const Color(0xFF2A2A3E),
  };

  Color _fg(String status) => switch (status) {
    'Pending' => const Color(0xFFFF8C00),
    'InPreparation' => const Color(0xFF60C0FF),
    'Ready' => const Color(0xFF4EE87A),
    _ => Colors.white54,
  };

  IconData _statusIcon(String status) => switch (status) {
    'Pending' => Icons.hourglass_empty_rounded,
    'InPreparation' => Icons.local_fire_department_rounded,
    'Ready' => Icons.check_circle_rounded,
    _ => Icons.circle_outlined,
  };

  @override
  Widget build(BuildContext context) {
    final bg = _bg(item.status);
    final fg = _fg(item.status);
    final updating = context.watch<StationProvider>().isUpdating(
      item.orderItemId,
    );
    final canAdvance =
        !updating &&
        (item.status == 'Pending' || item.status == 'InPreparation');
    final modifierLabels = item.modifierSelections
        .map((c) => c.label)
        .where((label) => label.isNotEmpty)
        .toList();

    return Padding(
      padding: const EdgeInsets.only(bottom: 2),
      child: Material(
        color: bg,
        borderRadius: BorderRadius.circular(4),
        child: InkWell(
          borderRadius: BorderRadius.circular(4),
          // Clic único = avanzar estado
          onTap: canAdvance
              ? () => context.read<StationProvider>().advanceItemStatus(item)
              : null,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 5),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    // Ícono de estado
                    SizedBox(
                      width: 10,
                      height: 10,
                      child: updating
                          ? CircularProgressIndicator(strokeWidth: 1, color: fg)
                          : Icon(_statusIcon(item.status), color: fg, size: 10),
                    ),
                    const SizedBox(width: 3),
                    // Cantidad
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 3,
                        vertical: 1,
                      ),
                      decoration: BoxDecoration(
                        color: fg.withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(2),
                      ),
                      child: Text(
                        'x${item.quantity}',
                        style: TextStyle(
                          color: fg,
                          fontWeight: FontWeight.bold,
                          fontSize: 9,
                        ),
                      ),
                    ),
                    const SizedBox(width: 4),
                    // Nombre
                    Expanded(
                      child: Text(
                        item.itemName,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 10,
                          fontWeight: FontWeight.w600,
                        ),
                        softWrap: true,
                      ),
                    ),
                    // Cursor visual si se puede tocar
                    if (canAdvance)
                      Icon(
                        Icons.touch_app_rounded,
                        color: fg.withValues(alpha: 0.5),
                        size: 9,
                      ),
                  ],
                ),
                // Modificadores (ej: salsa bbq, mostaza miel...)
                if (modifierLabels.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Padding(
                    padding: const EdgeInsets.only(left: 13),
                    child: Wrap(
                      spacing: 2,
                      runSpacing: 2,
                      children: modifierLabels
                          .map(
                            (label) => Container(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 3,
                                vertical: 1,
                              ),
                              decoration: BoxDecoration(
                                color: const Color(
                                  0xFF7C3AED,
                                ).withValues(alpha: 0.25),
                                borderRadius: BorderRadius.circular(2),
                                border: Border.all(
                                  color: const Color(
                                    0xFF7C3AED,
                                  ).withValues(alpha: 0.5),
                                ),
                              ),
                              child: Text(
                                label,
                                style: const TextStyle(
                                  color: Color(0xFFBB86FC),
                                  fontSize: 8,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          )
                          .toList(),
                    ),
                  ),
                ],

                if (item.isTakeout) ...[
                  const SizedBox(height: 3),
                  const Padding(
                    padding: EdgeInsets.only(left: 13),
                    child: Row(
                      children: [
                        Icon(
                          Icons.shopping_bag_rounded,
                          color: Colors.orangeAccent,
                          size: 9,
                        ),
                        SizedBox(width: 2),
                        Text(
                          'PARA LLEVAR',
                          style: TextStyle(
                            color: Colors.orangeAccent,
                            fontSize: 8,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],

                // Notas libres del mesero
                if (item.notes != null && item.notes!.isNotEmpty) ...[
                  const SizedBox(height: 3),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const SizedBox(width: 13),
                      const Icon(
                        Icons.notes_rounded,
                        color: Colors.amber,
                        size: 8,
                      ),
                      const SizedBox(width: 2),
                      Expanded(
                        child: Text(
                          item.notes!,
                          style: const TextStyle(
                            color: Colors.amber,
                            fontSize: 9,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
