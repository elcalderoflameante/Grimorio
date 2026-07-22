import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/station_provider.dart';
import '../models/completed_order.dart';
import '../models/station_item.dart';

class CompletedSidebar extends StatelessWidget {
  final double width;
  const CompletedSidebar({super.key, required this.width});

  @override
  Widget build(BuildContext context) {
    final completed = context.watch<StationProvider>().completedOrders;

    return Container(
      width: width,
      decoration: const BoxDecoration(
        color: Color(0xFF12122A),
        border: Border(left: BorderSide(color: Color(0xFF2A2A4A), width: 1)),
      ),
      child: Column(
        children: [
          // ── Header ─────────────────────────────────────────────────
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            decoration: const BoxDecoration(
              color: Color(0xFF0A0A1A),
              border: Border(bottom: BorderSide(color: Color(0xFF2A2A4A))),
            ),
            child: Row(
              children: [
                const Icon(Icons.check_circle_rounded, color: Color(0xFF4EE87A), size: 16),
                const SizedBox(width: 6),
                const Expanded(
                  child: Text(
                    'Completados',
                    style: TextStyle(
                        color: Colors.white70,
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 0.5),
                  ),
                ),
                if (completed.isNotEmpty)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                    decoration: BoxDecoration(
                      color: const Color(0xFF4EE87A).withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Text(
                      '${completed.length}',
                      style: const TextStyle(
                          color: Color(0xFF4EE87A),
                          fontWeight: FontWeight.bold,
                          fontSize: 11),
                    ),
                  ),
              ],
            ),
          ),

          // ── Lista con scroll (los más recientes arriba) ─────────────
          Expanded(
            child: completed.isEmpty
                ? const Center(
                    child: Text(
                      'Ninguno aún',
                      style: TextStyle(color: Colors.white24, fontSize: 12),
                    ),
                  )
                : ListView.separated(
                    padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 8),
                    itemCount: completed.length,
                    separatorBuilder: (context, index) => const SizedBox(height: 6),
                    itemBuilder: (_, i) => _CompletedTile(order: completed[i]),
                  ),
          ),
        ],
      ),
    );
  }
}

// ── Tile expandible ───────────────────────────────────────────────────────────

class _CompletedTile extends StatefulWidget {
  final CompletedOrder order;
  const _CompletedTile({required this.order});

  @override
  State<_CompletedTile> createState() => _CompletedTileState();
}

class _CompletedTileState extends State<_CompletedTile> {
  bool _expanded = false;

  String _elapsed() {
    final mins = DateTime.now().difference(widget.order.completedAt).inMinutes;
    if (mins < 1) return 'ahora';
    if (mins == 1) return 'hace 1 min';
    return 'hace $mins min';
  }

  String _orderTitle() {
    if (widget.order.orderLabel == '#${widget.order.orderNumber}') {
      return 'Pedido #${widget.order.orderNumber}';
    }
    return 'Pedido #${widget.order.orderNumber} - ${widget.order.orderLabel}';
  }

  String? _orderNotes() {
    final notes = widget.order.orderNotes?.trim();
    if (notes == null || notes.isEmpty) return null;
    return notes;
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF003D1A).withValues(alpha: 0.35),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: _expanded
              ? const Color(0xFF4EE87A).withValues(alpha: 0.5)
              : const Color(0xFF4EE87A).withValues(alpha: 0.2),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Fila principal ──────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Padding(
                  padding: EdgeInsets.only(top: 1),
                  child: Icon(Icons.check_rounded, color: Color(0xFF4EE87A), size: 13),
                ),
                const SizedBox(width: 5),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        _orderTitle(),
                        style: const TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.bold,
                            fontSize: 13),
                        overflow: TextOverflow.ellipsis,
                      ),
                      if (_orderNotes() != null) ...[
                        const SizedBox(height: 3),
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Icon(Icons.info_outline_rounded,
                                color: Colors.amberAccent, size: 11),
                            const SizedBox(width: 4),
                            Expanded(
                              child: Text(
                                _orderNotes()!,
                                style: const TextStyle(
                                    color: Colors.amberAccent,
                                    fontSize: 10,
                                    fontWeight: FontWeight.w600),
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ],
                        ),
                      ],
                      const SizedBox(height: 2),
                      Row(
                        children: [
                          Text(
                            '${widget.order.itemCount} ítem${widget.order.itemCount == 1 ? '' : 's'}',
                            style: const TextStyle(color: Colors.white38, fontSize: 11),
                          ),
                          const Spacer(),
                          Text(
                            _elapsed(),
                            style: const TextStyle(color: Colors.white24, fontSize: 10),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 6),
                // Ícono ojo para expandir/colapsar
                GestureDetector(
                  onTap: () => setState(() => _expanded = !_expanded),
                  child: Container(
                    padding: const EdgeInsets.all(5),
                    decoration: BoxDecoration(
                      color: _expanded
                          ? const Color(0xFF4EE87A).withValues(alpha: 0.15)
                          : Colors.transparent,
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Icon(
                      _expanded ? Icons.visibility_off_outlined : Icons.visibility_outlined,
                      color: _expanded ? const Color(0xFF4EE87A) : Colors.white30,
                      size: 16,
                    ),
                  ),
                ),
              ],
            ),
          ),

          // ── Detalle expandible ──────────────────────────────────────
          if (_expanded) ...[
            Container(
              height: 1,
              color: const Color(0xFF4EE87A).withValues(alpha: 0.15),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(10, 6, 10, 8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: widget.order.items
                    .map((item) => _ItemDetail(item: item))
                    .toList(),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ── Detalle de un ítem completado ─────────────────────────────────────────────

class _ItemDetail extends StatelessWidget {
  final StationItem item;
  const _ItemDetail({required this.item});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 5),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'x${item.quantity}',
            style: const TextStyle(
                color: Color(0xFF4EE87A),
                fontWeight: FontWeight.bold,
                fontSize: 12),
          ),
          const SizedBox(width: 6),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.itemName,
                  style: const TextStyle(color: Colors.white70, fontSize: 12),
                ),
                if (item.modifierSelections.isNotEmpty)
                  Text(
                    item.modifierSelections
                        .map((c) => c.label)
                        .join(', '),
                    style: const TextStyle(color: Color(0xFFBB86FC), fontSize: 11),
                  ),
                if (item.notes != null && item.notes!.isNotEmpty)
                  Text(
                    item.notes!,
                    style: const TextStyle(color: Colors.amber, fontSize: 11),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
