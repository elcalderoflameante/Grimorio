import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/network/api_error.dart';
import '../../../../core/theme/app_theme.dart';
import '../../data/models/order_models.dart';
import '../../data/services/order_api_service.dart';
import 'new_order_page.dart';

class TableAccountPage extends ConsumerStatefulWidget {
  const TableAccountPage({super.key, required this.table});

  final TableDto table;

  @override
  ConsumerState<TableAccountPage> createState() => _TableAccountPageState();
}

class _TableAccountPageState extends ConsumerState<TableAccountPage> {
  OrderDto? _order;
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadOrder();
  }

  Future<void> _loadOrder() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });
    try {
      final order = await ref
          .read(orderApiServiceProvider)
          .getOrder(widget.table.currentOrderId!);
      if (!mounted) return;
      setState(() {
        _order = order;
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _errorMessage = readableApiError(
          error,
          fallback: 'No se pudo cargar la cuenta.',
        );
      });
    }
  }

  Future<void> _addItems() async {
    final order = _order;
    if (order == null) return;
    await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => NewOrderPage(
          type: OrderType.dineIn,
          table: widget.table,
          orderId: order.id,
          orderIsDraft: order.status == OrderStatus.draft,
          draftOrder: order.status == OrderStatus.draft ? order : null,
        ),
      ),
    );
    await _loadOrder();
  }

  Future<void> _editItemNotes(OrderItemDto item) async {
    if (item.status != OrderItemStatus.pending) return;
    final controller = TextEditingController(text: item.notes ?? '');
    final notes = await showDialog<String?>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text('Observación · ${item.itemName}'),
        content: TextField(
          controller: controller,
          autofocus: true,
          maxLines: 3,
          decoration: const InputDecoration(
            hintText: 'Ej. sin cebolla, bien cocido…',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () =>
                Navigator.pop(dialogContext, controller.text.trim()),
            child: const Text('Guardar'),
          ),
        ],
      ),
    );
    controller.dispose();
    if (notes == null || !mounted) return;
    try {
      await ref
          .read(orderApiServiceProvider)
          .updateOrderItemNotes(item.id, notes.isEmpty ? null : notes);
      await _loadOrder();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            readableApiError(
              error,
              fallback: 'No se pudo actualizar la observación.',
            ),
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Cuenta · Mesa ${widget.table.code}'),
        actions: [
          IconButton(
            tooltip: 'Actualizar',
            onPressed: _loadOrder,
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: _buildBody(),
      bottomNavigationBar: _order == null
          ? null
          : SafeArea(
              top: false,
              minimum: const EdgeInsets.fromLTRB(16, 8, 16, 12),
              child: FilledButton.icon(
                onPressed: _addItems,
                icon: const Icon(Icons.add_shopping_cart_rounded),
                label: Text(
                  _order!.status == OrderStatus.draft
                      ? 'Continuar pedido'
                      : 'Agregar productos',
                ),
              ),
            ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(_errorMessage!, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              OutlinedButton.icon(
                onPressed: _loadOrder,
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Reintentar'),
              ),
            ],
          ),
        ),
      );
    }

    final order = _order!;
    return RefreshIndicator(
      onRefresh: _loadOrder,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(14, 14, 14, 24),
        children: [
          _AccountSummary(
            order: order,
            pendingTotal: widget.table.pendingPaymentTotal,
          ),
          const SizedBox(height: 16),
          Text(
            'Productos',
            style: GoogleFonts.cinzel(
              color: kGoldLight,
              fontSize: 15,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 8),
          ...order.items.map(
            (item) => _OrderItemCard(
              item: item,
              onEditNotes: item.status == OrderItemStatus.pending
                  ? () => _editItemNotes(item)
                  : null,
            ),
          ),
          if (order.notes?.isNotEmpty == true) ...[
            const SizedBox(height: 12),
            _NotesCard(notes: order.notes!),
          ],
        ],
      ),
    );
  }
}

class _AccountSummary extends StatelessWidget {
  const _AccountSummary({required this.order, required this.pendingTotal});

  final OrderDto order;
  final double pendingTotal;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Pedido #${order.number}',
                    style: GoogleFonts.cinzel(
                      color: kParchment,
                      fontSize: 17,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                Text(
                  order.status.label,
                  style: TextStyle(color: _statusColor(order.status)),
                ),
              ],
            ),
            const Divider(height: 24),
            _AmountRow(label: 'Subtotal', value: order.subtotal),
            if (order.discountTotal > 0) ...[
              const SizedBox(height: 8),
              _AmountRow(label: 'Descuentos', value: -order.discountTotal),
            ],
            if (order.taxableBase15 > 0) ...[
              const SizedBox(height: 8),
              _AmountRow(label: 'Base gravada', value: order.taxableBase15),
            ],
            if (order.taxableBase0 > 0) ...[
              const SizedBox(height: 8),
              _AmountRow(label: 'Base 0%', value: order.taxableBase0),
            ],
            if (order.taxableBaseExempt > 0) ...[
              const SizedBox(height: 8),
              _AmountRow(label: 'Base exenta', value: order.taxableBaseExempt),
            ],
            if (order.taxAmount > 0) ...[
              const SizedBox(height: 8),
              _AmountRow(label: 'Impuestos incluidos', value: order.taxAmount),
            ],
            const SizedBox(height: 8),
            _AmountRow(label: 'Total', value: order.total, emphasized: true),
            const SizedBox(height: 8),
            _AmountRow(
              label: 'Pendiente',
              value: pendingTotal,
              emphasized: true,
            ),
          ],
        ),
      ),
    );
  }
}

class _AmountRow extends StatelessWidget {
  const _AmountRow({
    required this.label,
    required this.value,
    this.emphasized = false,
  });

  final String label;
  final double value;
  final bool emphasized;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: TextStyle(color: emphasized ? kParchment : kParchmentDim),
          ),
        ),
        Text(
          '\$${value.toStringAsFixed(2)}',
          style: TextStyle(
            color: emphasized ? kGoldLight : kParchment,
            fontWeight: emphasized ? FontWeight.w800 : FontWeight.w500,
          ),
        ),
      ],
    );
  }
}

class _OrderItemCard extends StatelessWidget {
  const _OrderItemCard({required this.item, this.onEditNotes});

  final OrderItemDto item;
  final VoidCallback? onEditNotes;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 34,
              height: 34,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: kGold.withAlpha(25),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                '${item.quantity}×',
                style: const TextStyle(
                  color: kGoldLight,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.itemName,
                    style: const TextStyle(
                      color: kParchment,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  if (item.stationName?.isNotEmpty == true)
                    Text(
                      item.stationName!,
                      style: const TextStyle(
                        color: kParchmentDim,
                        fontSize: 11,
                      ),
                    ),
                  if (item.modifierSelections.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Text(
                        item.modifierSelections
                            .map((selection) => selection.label)
                            .join(', '),
                        style: const TextStyle(color: kGoldLight, fontSize: 12),
                      ),
                    ),
                  if (item.isTakeout)
                    const Padding(
                      padding: EdgeInsets.only(top: 4),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Icons.shopping_bag_rounded,
                            size: 14,
                            color: Color(0xFFFFAB00),
                          ),
                          SizedBox(width: 4),
                          Text(
                            'PARA LLEVAR',
                            style: TextStyle(
                              color: Color(0xFFFFAB00),
                              fontSize: 11,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ],
                      ),
                    ),
                  if (item.notes?.isNotEmpty == true)
                    Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Text(
                        item.notes!,
                        style: const TextStyle(
                          color: kParchmentDim,
                          fontSize: 12,
                        ),
                      ),
                    ),
                  Padding(
                    padding: const EdgeInsets.only(top: 6),
                    child: Row(
                      children: [
                        _ItemStatusBadge(status: item.status),
                        if (onEditNotes != null) ...[
                          const SizedBox(width: 8),
                          InkWell(
                            onTap: onEditNotes,
                            child: const Padding(
                              padding: EdgeInsets.symmetric(vertical: 6),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(
                                    Icons.edit_note_rounded,
                                    size: 18,
                                    color: kGoldLight,
                                  ),
                                  SizedBox(width: 3),
                                  Text(
                                    'Observación',
                                    style: TextStyle(
                                      color: kGoldLight,
                                      fontSize: 12,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Text(
              '\$${item.totalPrice.toStringAsFixed(2)}',
              style: const TextStyle(
                color: kGoldLight,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ItemStatusBadge extends StatelessWidget {
  const _ItemStatusBadge({required this.status});

  final OrderItemStatus status;

  @override
  Widget build(BuildContext context) {
    final (label, color) = switch (status) {
      OrderItemStatus.pending => ('Pendiente', const Color(0xFF9E9E9E)),
      OrderItemStatus.inPreparation => (
        'En preparación',
        const Color(0xFFFFAB00),
      ),
      OrderItemStatus.ready => ('Listo', const Color(0xFF69F0AE)),
      OrderItemStatus.cancelled => ('Cancelado', const Color(0xFFFF6B6B)),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withAlpha(25),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withAlpha(140)),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _NotesCard extends StatelessWidget {
  const _NotesCard({required this.notes});

  final String notes;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: const Icon(Icons.notes_rounded, color: kGoldLight),
        title: const Text('Observación general'),
        subtitle: Text(notes),
      ),
    );
  }
}

Color _statusColor(OrderStatus status) => switch (status) {
  OrderStatus.draft => const Color(0xFF9E9E9E),
  OrderStatus.confirmed => const Color(0xFF40C4FF),
  OrderStatus.inPreparation => const Color(0xFFFFAB00),
  OrderStatus.ready => const Color(0xFF69F0AE),
  OrderStatus.delivered => kGoldLight,
  OrderStatus.cancelled => const Color(0xFFFF6B6B),
};
