import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/station_item.dart';
import '../providers/station_provider.dart';

// Ancho fijo de la tarjeta — en un monitor de 19" landscape (1920px)
// con sidebar de 220px y padding, caben cómodamente 5-6 tarjetas por fila.
const double kCardWidth = 290.0;

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
    if (t.contains('mesa') || t.contains('dine')) return const Color(0xFF3B82F6);
    if (t.contains('llevar') || t.contains('takeout')) return const Color(0xFFF59E0B);
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
      width: kCardWidth,
      decoration: BoxDecoration(
        color: const Color(0xFF1C1C30),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _cardBorderColor(), width: 1.5),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          // ── Header ───────────────────────────────────────────────────
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: const BoxDecoration(
              color: Color(0xFF12122A),
              borderRadius: BorderRadius.vertical(top: Radius.circular(9)),
            ),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    _orderTitle(),
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                      fontSize: 15,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                const SizedBox(width: 8),
                // Badge tipo
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: _typeBadgeColor().withValues(alpha: 0.2),
                    borderRadius: BorderRadius.circular(4),
                    border: Border.all(color: _typeBadgeColor().withValues(alpha: 0.5)),
                  ),
                  child: Text(
                    _orderTypeBadge(),
                    style: TextStyle(color: _typeBadgeColor(), fontSize: 10, fontWeight: FontWeight.bold),
                  ),
                ),
                const SizedBox(width: 8),
                // Tiempo transcurrido
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.schedule, size: 12, color: _elapsedColor()),
                    const SizedBox(width: 3),
                    Text(_elapsed(), style: TextStyle(color: _elapsedColor(), fontSize: 11)),
                  ],
                ),
              ],
            ),
          ),

          // ── Ítems ─────────────────────────────────────────────────────
          if (_orderNotes() != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 8, 12, 2),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.info_outline_rounded,
                      color: Colors.amberAccent, size: 14),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      _orderNotes()!,
                      style: const TextStyle(
                        color: Colors.amberAccent,
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
            ),

          Padding(
            padding: const EdgeInsets.all(6),
            child: Column(
              children: items
                  .map((item) => _ItemRow(item: item))
                  .toList(),
            ),
          ),
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
    final canAdvance = item.status == 'Pending' || item.status == 'InPreparation';

    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Material(
        color: bg,
        borderRadius: BorderRadius.circular(7),
        child: InkWell(
          borderRadius: BorderRadius.circular(7),
          // Clic único = avanzar estado
          onTap: canAdvance
              ? () => context.read<StationProvider>().advanceItemStatus(item)
              : null,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 9),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    // Ícono de estado
                    Icon(_statusIcon(item.status), color: fg, size: 16),
                    const SizedBox(width: 6),
                    // Cantidad
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                      decoration: BoxDecoration(
                        color: fg.withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        'x${item.quantity}',
                        style: TextStyle(
                            color: fg, fontWeight: FontWeight.bold, fontSize: 13),
                      ),
                    ),
                    const SizedBox(width: 8),
                    // Nombre
                    Expanded(
                      child: Text(
                        item.itemName,
                        style: const TextStyle(
                            color: Colors.white,
                            fontSize: 14,
                            fontWeight: FontWeight.w600),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    // Cursor visual si se puede tocar
                    if (canAdvance)
                      Icon(Icons.touch_app_rounded, color: fg.withValues(alpha: 0.5), size: 14),
                  ],
                ),
                // Ingredientes variables (ej: salsa bbq, mostaza miel…)
                if (item.ingredientChoices.isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Padding(
                    padding: const EdgeInsets.only(left: 22),
                    child: Wrap(
                      spacing: 4,
                      runSpacing: 4,
                      children: item.ingredientChoices
                          .map((c) => Container(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 6, vertical: 2),
                                decoration: BoxDecoration(
                                  color: const Color(0xFF7C3AED).withValues(alpha: 0.25),
                                  borderRadius: BorderRadius.circular(4),
                                  border: Border.all(
                                      color: const Color(0xFF7C3AED).withValues(alpha: 0.5)),
                                ),
                                child: Text(
                                  c.chosenArticleName,
                                  style: const TextStyle(
                                      color: Color(0xFFBB86FC),
                                      fontSize: 11,
                                      fontWeight: FontWeight.w600),
                                ),
                              ))
                          .toList(),
                    ),
                  ),
                ],

                // Notas libres del mesero
                if (item.notes != null && item.notes!.isNotEmpty) ...[
                  const SizedBox(height: 5),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const SizedBox(width: 22),
                      const Icon(Icons.notes_rounded, color: Colors.amber, size: 12),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          item.notes!,
                          style: const TextStyle(color: Colors.amber, fontSize: 12),
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
